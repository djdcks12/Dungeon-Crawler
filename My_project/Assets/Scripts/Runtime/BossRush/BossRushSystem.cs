using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 보스 러쉬 / 무한 모드
    /// 연속 보스 웨이브, 점점 강해지는 보스, 보스 간 30초 휴식
    /// 기록 경쟁, 웨이브별 보상
    /// </summary>
    public class BossRushSystem : NetworkBehaviour
    {
        public static BossRushSystem Instance { get; private set; }

        [Header("보스 러쉬 설정")]
        [SerializeField] private float restTimeBetweenWaves = 30f;
        [SerializeField] private float bossTimeLimit = 300f; // 5분 시간제한
        [SerializeField] private int bonusWaveInterval = 5; // 5웨이브마다 보너스

        [Header("난이도 스케일링")]
        [SerializeField] private float hpScalePerWave = 0.25f; // 웨이브당 HP +25%
        [SerializeField] private float dmgScalePerWave = 0.15f; // 웨이브당 데미지 +15%
        [SerializeField] private float defScalePerWave = 0.10f; // 웨이브당 방어 +10%

        // 보스 러쉬 상태
        private NetworkVariable<bool> isRushActive = new NetworkVariable<bool>(false);
        private NetworkVariable<int> currentWave = new NetworkVariable<int>(0);
        private NetworkVariable<float> bossTimer = new NetworkVariable<float>(0);
        private NetworkVariable<float> restTimer = new NetworkVariable<float>(0);
        private NetworkVariable<bool> isResting = new NetworkVariable<bool>(false);

        // 서버 데이터
        private long currentBossHP;
        private long currentBossMaxHP;
        private Dictionary<ulong, BossRushPlayerData> playerData = new Dictionary<ulong, BossRushPlayerData>();
        private string[] bossPool; // 보스 이름 풀
        private string currentBossName;

        // 로컬
        private int localBestWave;
        private long localBestScore;

        // 이벤트
        public System.Action<int> OnWaveStarted; // wave number
        public System.Action OnRestStarted;
        public System.Action<int, long> OnBossRushEnded; // final wave, score
        public System.Action<long, long> OnBossHPChanged; // current, max
        public System.Action<float> OnTimerUpdated;

        // 접근자
        public bool IsActive => isRushActive.Value;
        public int CurrentWave => currentWave.Value;
        public bool IsResting => isResting.Value;
        public float RemainingTime => isResting.Value ? restTimer.Value : bossTimer.Value;
        public string CurrentBossName => currentBossName ?? "???";
        public long BossHP => currentBossHP;
        public long BossMaxHP => currentBossMaxHP;
        public float BossHPPercent => currentBossMaxHP > 0 ? (float)currentBossHP / currentBossMaxHP : 0;
        public int LocalBestWave => localBestWave;
        public long LocalBestScore => localBestScore;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            InitBossPool();
            LoadBestRecord();
        }

        private void InitBossPool()
        {
            bossPool = new string[]
            {
                "고대 골렘", "심연의 크라켄", "화염 군주", "공허의 드래곤", "마왕 아자젤",
                "서리 거인", "독액 히드라", "암흑 기사", "붉은 와이번", "그림자 군주",
                "대지의 정령왕", "폭풍의 피닉스", "죽음의 리치", "기계 타이탄", "혼돈의 마수",
                "용암 골렘", "빙결 드레이크", "번개 크라켄", "어둠의 세라핌", "최종 심판자"
            };
        }

        private void LoadBestRecord()
        {
            localBestWave = PlayerPrefs.GetInt("BossRush_BestWave", 0);
            long.TryParse(PlayerPrefs.GetString("BossRush_BestScore", "0"), out localBestScore);
        }

        private void SaveBestRecord()
        {
            PlayerPrefs.SetInt("BossRush_BestWave", localBestWave);
            PlayerPrefs.SetString("BossRush_BestScore", localBestScore.ToString());
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (!IsServer || !isRushActive.Value) return;

            if (isResting.Value)
            {
                // 휴식 시간 카운트다운
                restTimer.Value -= Time.deltaTime;
                if (restTimer.Value <= 0)
                {
                    isResting.Value = false;
                    StartNextWave();
                }
            }
            else if (currentWave.Value > 0)
            {
                // 보스 시간 제한
                bossTimer.Value -= Time.deltaTime;
                if (bossTimer.Value <= 0)
                {
                    // 시간 초과 → 러쉬 종료
                    EndBossRush("시간 초과!");
                }

                // HP 동기화 (매 30프레임)
                if (Time.frameCount % 30 == 0)
                {
                    UpdateBossHPClientRpc(currentBossHP, currentBossMaxHP, currentBossName ?? "");
                }
            }
        }

        #region 시작/종료

        /// <summary>
        /// 보스 러쉬 시작
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartBossRushServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (isRushActive.Value)
            {
                SendMessageClientRpc("이미 보스 러쉬가 진행 중입니다.", clientId);
                return;
            }

            // 레벨 확인
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;
            if (statsData.CurrentLevel < 5)
            {
                SendMessageClientRpc("레벨 5 이상에서 보스 러쉬를 시작할 수 있습니다.", clientId);
                return;
            }

            // 입장료
            long entryCost = 10000;
            if (statsData.Gold < entryCost)
            {
                SendMessageClientRpc($"골드 부족 (입장료: {entryCost:N0}G)", clientId);
                return;
            }
            statsData.ChangeGold(-entryCost);

            // 초기화
            playerData.Clear();
            isRushActive.Value = true;
            currentWave.Value = 0;
            isResting.Value = false;

            // 모든 접속 클라이언트를 참여자로 등록
            if (NetworkManager.Singleton == null) return;
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                playerData[kvp.Key] = new BossRushPlayerData
                {
                    totalDamage = 0,
                    bossesKilled = 0,
                    score = 0
                };
            }

            NotifyBossRushStartClientRpc();
            StartNextWave();
        }

        /// <summary>
        /// 보스 러쉬 포기
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ForfeitBossRushServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!isRushActive.Value) return;
            EndBossRush("포기");
        }

        private void EndBossRush(string reason)
        {
            if (!IsServer || !isRushActive.Value) return;

            int finalWave = currentWave.Value;
            isRushActive.Value = false;
            isResting.Value = false;

            // 보상 분배
            foreach (var kvp in playerData)
            {
                DistributeRewards(kvp.Key, kvp.Value, finalWave);
            }

            NotifyBossRushEndClientRpc(finalWave, reason);
        }

        #endregion

        #region 웨이브 관리

        private void StartNextWave()
        {
            currentWave.Value++;
            int wave = currentWave.Value;

            // 보스 선택 (순환)
            int bossIdx = (wave - 1) % bossPool.Length;
            currentBossName = bossPool[bossIdx];

            // 보스 스탯 스케일링
            long baseHP = 50000 + (wave * 10000);
            currentBossMaxHP = (long)(baseHP * (1f + hpScalePerWave * (wave - 1)));
            currentBossHP = currentBossMaxHP;

            // 타이머 리셋
            bossTimer.Value = bossTimeLimit;

            // 보너스 웨이브 (5의 배수)
            bool isBonus = wave % bonusWaveInterval == 0;
            if (isBonus)
            {
                currentBossMaxHP = (long)(currentBossMaxHP * 2.0f);
                currentBossHP = currentBossMaxHP;
                currentBossName = "★ " + currentBossName + " ★";
            }

            NotifyWaveStartClientRpc(wave, currentBossName, currentBossMaxHP, isBonus);
        }

        /// <summary>
        /// 보스에게 데미지
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DealDamageToBossServerRpc(long damage, ServerRpcParams rpcParams = default)
        {
            if (!isRushActive.Value || isResting.Value) return;
            if (currentBossHP <= 0) return;

            ulong clientId = rpcParams.Receive.SenderClientId;

            // 데미지 적용
            long clampedDmg = damage < currentBossHP ? damage : currentBossHP;
            currentBossHP -= clampedDmg;

            // 기여도 기록
            if (playerData.ContainsKey(clientId))
            {
                playerData[clientId].totalDamage += clampedDmg;
                playerData[clientId].score += clampedDmg;
            }

            // 보스 사망 확인
            if (currentBossHP <= 0)
            {
                OnBossDefeated();
            }
        }

        private void OnBossDefeated()
        {
            int wave = currentWave.Value;

            // 기여자 보스킬 카운트 증가
            foreach (var kvp in playerData)
            {
                if (kvp.Value.totalDamage > 0)
                    kvp.Value.bossesKilled++;
            }

            // 웨이브 보상 (즉시)
            bool isBonus = wave % bonusWaveInterval == 0;
            long waveGold = 1000L * wave * (isBonus ? 3 : 1);
            long waveExp = 500L * wave * (isBonus ? 3 : 1);

            foreach (var kvp in playerData)
            {
                var stats = GetPlayerStatsData(kvp.Key);
                if (stats != null)
                {
                    stats.ChangeGold(waveGold);
                    stats.AddExperience(waveExp);
                }

                // 스코어 보너스
                kvp.Value.score += waveGold + waveExp;
            }

            NotifyBossDefeatedClientRpc(wave, waveGold, waveExp, isBonus);

            // 휴식 시작
            isResting.Value = true;
            restTimer.Value = restTimeBetweenWaves;
        }

        #endregion

        #region 보상

        private void DistributeRewards(ulong clientId, BossRushPlayerData data, int finalWave)
        {
            var statsData = GetPlayerStatsData(clientId);
            if (statsData == null) return;

            // 최종 보상
            long goldReward = 5000L * finalWave + data.bossesKilled * 2000L;
            long expReward = 2000L * finalWave + data.totalDamage / 1000;

            statsData.ChangeGold(goldReward);
            statsData.AddExperience(expReward);

            // 보너스 아이템 (10웨이브 이상)
            if (finalWave >= 10 && MailSystem.Instance != null)
            {
                MailSystem.Instance.SendSystemMail(
                    clientId, "보스 러쉬 보상",
                    $"보스 러쉬 {finalWave}웨이브 달성!\n최종 점수: {data.score:N0}"
                );
            }

            // 업적
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.NotifyEvent(AchievementEvent.BossKill, data.bossesKilled);

            // 리더보드 (던전 클리어 카테고리 활용)
            if (LeaderboardSystem.Instance != null)
            {
                var statsForName = GetPlayerStatsData(clientId);
                string pName = statsForName != null ? statsForName.CharacterName : "Unknown";
                LeaderboardSystem.Instance.UpdateScore(clientId, pName, LeaderboardCategory.BossKills, data.bossesKilled);
            }

            // 시즌패스
            if (SeasonPassSystem.Instance != null)
                SeasonPassSystem.Instance.AddSeasonExp(clientId, finalWave * 20, "boss_rush");
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyBossRushStartClientRpc()
        {
            OnWaveStarted?.Invoke(0);
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification("<color=#FF4444>보스 러쉬 시작!</color>", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyWaveStartClientRpc(int wave, string bossName, long maxHP, bool isBonus)
        {
            currentBossName = bossName;
            currentBossMaxHP = maxHP;
            currentBossHP = maxHP;

            OnWaveStarted?.Invoke(wave);
            OnBossHPChanged?.Invoke(maxHP, maxHP);

            var notif = NotificationManager.Instance;
            if (notif != null)
            {
                string msg = isBonus
                    ? $"<color=#FFD700>★ 보너스 웨이브 {wave}! ★</color> {bossName}"
                    : $"웨이브 {wave}: {bossName} 출현!";
                notif.ShowNotification(msg, isBonus ? NotificationType.Achievement : NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifyBossDefeatedClientRpc(int wave, long gold, long exp, bool isBonus)
        {
            OnRestStarted?.Invoke();

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"웨이브 {wave} 클리어! +{gold:N0}G +{exp:N0}EXP", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBossRushEndClientRpc(int finalWave, string reason)
        {
            // 로컬 최고 기록 갱신
            if (finalWave > localBestWave)
            {
                localBestWave = finalWave;
                SaveBestRecord();
            }

            OnBossRushEnded?.Invoke(finalWave, 0);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"보스 러쉬 종료! (웨이브 {finalWave}, {reason})", NotificationType.Warning);
        }

        [ClientRpc]
        private void UpdateBossHPClientRpc(long hp, long maxHP, string bossName)
        {
            currentBossHP = hp;
            currentBossMaxHP = maxHP;
            if (!string.IsNullOrEmpty(bossName))
                currentBossName = bossName;
            OnBossHPChanged?.Invoke(hp, maxHP);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(message, NotificationType.Warning);
        }

        #endregion

        #region Utility

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        #endregion

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnWaveStarted = null;
                OnRestStarted = null;
                OnBossRushEnded = null;
                OnBossHPChanged = null;
                OnTimerUpdated = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// 보스 러쉬 플레이어 데이터
    /// </summary>
    public class BossRushPlayerData
    {
        public long totalDamage;
        public int bossesKilled;
        public long score;
    }
}
