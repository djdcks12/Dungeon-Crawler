using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum WeatherType { Clear, Rain, Snow, Fog, Storm, BloodRain }

    public struct WeatherModifiers
    {
        public float lightningDamageMod;
        public float moveSpeedMod;
        public float visionMod;
        public float allDamageMod;
        public float lifeStealMod;
    }

    [System.Serializable]
    public class WeatherEffect
    {
        public WeatherType type;
        public string name;
        public string description;
        public float lightningDmgMod;
        public float moveSpeedMod;
        public float visionMod;
        public float allDamageMod;
        public float lifeStealMod;
        public float durationMin;
        public float durationMax;
    }

    /// <summary>
    /// 날씨 시스템 - 서버 권한, 클라이언트 동기화
    /// 6가지 날씨 타입, 전투 보정, 특수 이벤트
    /// </summary>
    public class WeatherSystem : NetworkBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        // 네트워크 동기화
        private NetworkVariable<int> syncedWeatherType = new NetworkVariable<int>(0);

        // 서버 상태
        private WeatherType currentWeather = WeatherType.Clear;
        private float weatherEndTime;
        private float nextWeatherChangeTime;

        // 로컬 상태
        private WeatherType localCurrentWeather = WeatherType.Clear;
        private float localWeatherEndTime;
        private WeatherModifiers localModifiers;

        // 이벤트
        public System.Action<WeatherType> OnWeatherChanged;

        // 날씨 효과 정의
        private Dictionary<WeatherType, WeatherEffect> weatherEffects;

        // 가중치 (Clear 40%, Rain 20%, Snow 15%, Fog 12%, Storm 10%, BloodRain 3%)
        private static readonly (WeatherType type, float weight)[] weatherWeights = new[]
        {
            (WeatherType.Clear, 0.40f),
            (WeatherType.Rain, 0.20f),
            (WeatherType.Snow, 0.15f),
            (WeatherType.Fog, 0.12f),
            (WeatherType.Storm, 0.10f),
            (WeatherType.BloodRain, 0.03f)
        };

        // 스톰 번개
        private float nextLightningTime;
        [SerializeField] private float lightningIntervalMin = 5f;
        [SerializeField] private float lightningIntervalMax = 15f;
        [SerializeField] private float lightningAoeDamage = 25f;
        [SerializeField] private float lightningAoeRadius = 3f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeWeatherEffects();
        }

        private void InitializeWeatherEffects()
        {
            weatherEffects = new Dictionary<WeatherType, WeatherEffect>
            {
                { WeatherType.Clear, new WeatherEffect {
                    type = WeatherType.Clear, name = "맑음", description = "화창한 날씨",
                    lightningDmgMod = 0f, moveSpeedMod = 0f, visionMod = 0f,
                    allDamageMod = 0f, lifeStealMod = 0f, durationMin = 300f, durationMax = 600f } },
                { WeatherType.Rain, new WeatherEffect {
                    type = WeatherType.Rain, name = "비", description = "번개 피해가 증가합니다",
                    lightningDmgMod = 0.15f, moveSpeedMod = 0f, visionMod = 0f,
                    allDamageMod = 0f, lifeStealMod = 0f, durationMin = 180f, durationMax = 360f } },
                { WeatherType.Snow, new WeatherEffect {
                    type = WeatherType.Snow, name = "눈", description = "이동속도가 감소합니다",
                    lightningDmgMod = 0f, moveSpeedMod = -0.10f, visionMod = 0f,
                    allDamageMod = 0f, lifeStealMod = 0f, durationMin = 180f, durationMax = 300f } },
                { WeatherType.Fog, new WeatherEffect {
                    type = WeatherType.Fog, name = "안개", description = "시야가 감소합니다",
                    lightningDmgMod = 0f, moveSpeedMod = 0f, visionMod = -0.30f,
                    allDamageMod = 0f, lifeStealMod = 0f, durationMin = 120f, durationMax = 240f } },
                { WeatherType.Storm, new WeatherEffect {
                    type = WeatherType.Storm, name = "폭풍", description = "모든 피해와 번개 피해가 증가합니다",
                    lightningDmgMod = 0.25f, moveSpeedMod = 0f, visionMod = 0f,
                    allDamageMod = 0.20f, lifeStealMod = 0f, durationMin = 120f, durationMax = 180f } },
                { WeatherType.BloodRain, new WeatherEffect {
                    type = WeatherType.BloodRain, name = "핏빛 비", description = "생명력 흡수가 증가하고 전설 몬스터 출현 확률이 상승합니다",
                    lightningDmgMod = 0f, moveSpeedMod = 0f, visionMod = 0f,
                    allDamageMod = 0f, lifeStealMod = 0.10f, durationMin = 60f, durationMax = 120f } }
            };
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                float duration = Random.Range(300f, 600f);
                weatherEndTime = Time.time + duration;
                nextWeatherChangeTime = weatherEndTime;
            }

            syncedWeatherType.OnValueChanged += OnSyncedWeatherTypeChanged;

            int weatherVal = syncedWeatherType.Value;
            localCurrentWeather = System.Enum.IsDefined(typeof(WeatherType), weatherVal) ? (WeatherType)weatherVal : WeatherType.Clear;
            UpdateLocalModifiers();
        }

        public override void OnNetworkDespawn()
        {
            syncedWeatherType.OnValueChanged -= OnSyncedWeatherTypeChanged;
            base.OnNetworkDespawn();
        }

        private void OnSyncedWeatherTypeChanged(int oldVal, int newVal)
        {
            localCurrentWeather = (WeatherType)newVal;
            UpdateLocalModifiers();
            OnWeatherChanged?.Invoke(localCurrentWeather);
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsServer)
            {
                if (Time.time >= nextWeatherChangeTime)
                    ChangeWeatherServer();

                if (currentWeather == WeatherType.Storm)
                    HandleStormLightning();
            }
        }

        private void ChangeWeatherServer()
        {
            WeatherType newWeather = PickWeightedRandom();
            currentWeather = newWeather;

            var effect = weatherEffects[newWeather];
            float duration = Random.Range(effect.durationMin, effect.durationMax);
            weatherEndTime = Time.time + duration;
            nextWeatherChangeTime = weatherEndTime;

            syncedWeatherType.Value = (int)newWeather;
            NotifyWeatherChangedClientRpc((int)newWeather, effect.name, duration);

            if (newWeather == WeatherType.Storm)
                nextLightningTime = Time.time + Random.Range(lightningIntervalMin, lightningIntervalMax);
        }

        private WeatherType PickWeightedRandom()
        {
            float roll = Random.value;
            float cumulative = 0f;

            foreach (var entry in weatherWeights)
            {
                cumulative += entry.weight;
                if (roll <= cumulative)
                    return entry.type;
            }

            return WeatherType.Clear;
        }

        // 번개 AoE 충돌 감지 버퍼 (GC 방지)
        private static readonly Collider2D[] s_LightningBuffer = new Collider2D[16];

        private void HandleStormLightning()
        {
            if (Time.time < nextLightningTime) return;

            nextLightningTime = Time.time + Random.Range(lightningIntervalMin, lightningIntervalMax);

            // 랜덤 위치에 번개 낙뢰 (플레이어 근처)
            var netManager = NetworkManager.Singleton;
            if (netManager == null || netManager.ConnectedClientsList.Count == 0) return;

            var clientList = netManager.ConnectedClientsList;
            var targetClient = clientList[Random.Range(0, clientList.Count)];
            if (targetClient.PlayerObject == null) return;

            Vector3 strikePos = targetClient.PlayerObject.transform.position + new Vector3(
                Random.Range(-8f, 8f), Random.Range(-8f, 8f), 0f);

            // AoE 피해 (2D - OverlapCircleNonAlloc)
            int hitCount = Physics2D.OverlapCircleNonAlloc(strikePos, lightningAoeRadius, s_LightningBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var netObj = s_LightningBuffer[i].GetComponent<NetworkBehaviour>();
                if (netObj != null)
                    Debug.Log($"[WeatherSystem] Lightning strike at {strikePos}, hit {s_LightningBuffer[i].name}, dmg={lightningAoeDamage}");
            }
        }

        private void UpdateLocalModifiers()
        {
            if (!weatherEffects.ContainsKey(localCurrentWeather))
            {
                localModifiers = default;
                return;
            }

            var effect = weatherEffects[localCurrentWeather];
            localModifiers = new WeatherModifiers
            {
                lightningDamageMod = effect.lightningDmgMod,
                moveSpeedMod = effect.moveSpeedMod,
                visionMod = effect.visionMod,
                allDamageMod = effect.allDamageMod,
                lifeStealMod = effect.lifeStealMod
            };

            localWeatherEndTime = Time.time + Random.Range(effect.durationMin, effect.durationMax);
        }

        // === Public API ===

        public WeatherModifiers GetCurrentWeatherModifiers() => localModifiers;

        public WeatherType GetCurrentWeather() => localCurrentWeather;

        public float GetRemainingWeatherTime()
        {
            if (IsServer)
                return Mathf.Max(0f, weatherEndTime - Time.time);
            return Mathf.Max(0f, localWeatherEndTime - Time.time);
        }

        /// <summary>
        /// BloodRain 중 전설 몬스터 출현 확률 보너스 (+5%)
        /// </summary>
        public float GetLegendarySpawnBonus()
        {
            return localCurrentWeather == WeatherType.BloodRain ? 0.05f : 0f;
        }

        // === Network RPCs ===

        [ServerRpc(RequireOwnership = false)]
        public void RequestWeatherInfoServerRpc(ServerRpcParams rpcParams = default)
        {
            var effect = weatherEffects[currentWeather];
            float remaining = Mathf.Max(0f, weatherEndTime - Time.time);

            var clientId = rpcParams.Receive.SenderClientId;
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            NotifyWeatherChangedClientRpc((int)currentWeather, effect.name, remaining);
        }

        [ClientRpc]
        public void NotifyWeatherChangedClientRpc(int weatherTypeInt, string weatherName, float duration)
        {
            var wType = (WeatherType)weatherTypeInt;
            string msg;

            if (wType == WeatherType.BloodRain)
                msg = $"[경고] {weatherName}이 내리기 시작합니다! 전설 몬스터 출현 확률 증가! ({duration:F0}초)";
            else if (wType == WeatherType.Storm)
                msg = $"[경고] {weatherName}이 몰려옵니다! 번개에 주의하세요! ({duration:F0}초)";
            else if (wType == WeatherType.Clear)
                msg = "날씨가 맑아졌습니다.";
            else
                msg = $"{weatherName} 날씨가 시작됩니다. ({duration:F0}초)";

            // NotificationType 분기: BloodRain과 Storm은 Warning, 나머지는 System
            if (wType == WeatherType.BloodRain || wType == WeatherType.Storm)
                Debug.Log($"[WeatherSystem] WARNING: {msg}");
            else
                Debug.Log($"[WeatherSystem] {msg}");

            // NotificationManager 연동
            // NotificationManager.Instance?.ShowNotification(msg, wType == WeatherType.BloodRain || wType == WeatherType.Storm
            //     ? NotificationType.Warning : NotificationType.System);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnWeatherChanged = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
