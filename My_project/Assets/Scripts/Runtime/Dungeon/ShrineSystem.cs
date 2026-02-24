using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 제단/축복 시스템 - 디아블로식 제단 버프
    /// 일반 제단 (2분 지속) + 상위 제단 (30초 강력)
    /// </summary>
    public class ShrineSystem : MonoBehaviour
    {
        public static ShrineSystem Instance { get; private set; }

        // 활성 제단 버프
        private List<ShrineBuffInstance> activeShrineBufss = new List<ShrineBuffInstance>();

        // 이벤트
        public System.Action<ShrineType, float> OnShrineActivated;
        public System.Action<ShrineType> OnShrineExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // 버프 시간 업데이트
            for (int i = activeShrineBufss.Count - 1; i >= 0; i--)
            {
                var buff = activeShrineBufss[i];
                buff.remainingTime -= Time.deltaTime;

                if (buff.remainingTime <= 0f)
                {
                    OnShrineExpired?.Invoke(buff.type);

                    if (NotificationManager.Instance != null)
                    {
                        NotificationManager.Instance.ShowNotification(
                            $"제단 효과 종료: {GetShrineName(buff.type)}",
                            NotificationType.Warning
                        );
                    }

                    activeShrineBufss.RemoveAt(i);
                }
                else
                {
                    activeShrineBufss[i] = buff;
                }
            }
        }

        /// <summary>
        /// 제단 활성화
        /// </summary>
        public void ActivateShrine(ShrineType type, bool isPylon = false)
        {
            // 같은 타입 중복 제거
            activeShrineBufss.RemoveAll(b => b.type == type);

            float duration = isPylon ? 30f : 120f;
            float value = GetShrineValue(type, isPylon);

            activeShrineBufss.Add(new ShrineBuffInstance
            {
                type = type,
                value = value,
                remainingTime = duration,
                totalDuration = duration,
                isPylon = isPylon
            });

            OnShrineActivated?.Invoke(type, duration);

            // 알림
            string pylonText = isPylon ? " [상위]" : "";
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    $"제단 활성화{pylonText}: {GetShrineName(type)} ({duration}초)",
                    NotificationType.System
                );
            }

            // 이펙트
            if (CameraEffects.Instance != null)
            {
                CameraEffects.Instance.FlashGold(0.4f);
                CameraEffects.Instance.ShakeLight();
            }

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("Shrine_Activate");

            Debug.Log($"[Shrine] Activated: {type} (Pylon={isPylon}, Duration={duration}s, Value={value})");
        }

        // === 전투 시스템 연동 API ===

        /// <summary>
        /// 데미지 배율 (공격력 제단)
        /// </summary>
        public float GetDamageMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Power)
                    mult += buff.value / 100f;
                else if (buff.type == ShrineType.Frenzy)
                    mult += buff.value / 200f; // 광분: 공격속도+공격력 절반씩
            }
            return mult;
        }

        /// <summary>
        /// 피해 감소 배율 (보호 제단)
        /// </summary>
        public float GetDamageReduction()
        {
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Protection)
                    return buff.value / 100f;
                if (buff.type == ShrineType.Invincibility)
                    return 1f; // 완전 무적
            }
            return 0f;
        }

        /// <summary>
        /// 이동속도 배율 (속도 제단)
        /// </summary>
        public float GetSpeedMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Speed)
                    mult += buff.value / 100f;
            }
            return mult;
        }

        /// <summary>
        /// 공격속도 배율 (광분 제단)
        /// </summary>
        public float GetAttackSpeedMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Frenzy)
                    mult += buff.value / 100f;
            }
            return mult;
        }

        /// <summary>
        /// 경험치 배율 (깨달음 제단)
        /// </summary>
        public float GetExpMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Enlightenment)
                    mult += buff.value / 100f;
            }
            return mult;
        }

        /// <summary>
        /// 골드 배율 (행운 제단)
        /// </summary>
        public float GetGoldMultiplier()
        {
            float mult = 1f;
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Fortune)
                    mult += buff.value / 100f;
            }
            return mult;
        }

        /// <summary>
        /// 쿨다운 감소 (채널링 제단)
        /// </summary>
        public float GetCooldownReduction()
        {
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Channeling)
                    return buff.value / 100f;
            }
            return 0f;
        }

        /// <summary>
        /// 자원 비용 감소 (채널링 제단)
        /// </summary>
        public float GetResourceCostReduction()
        {
            foreach (var buff in activeShrineBufss)
            {
                if (buff.type == ShrineType.Channeling)
                    return buff.isPylon ? 1f : 0.5f; // 상위=무비용, 일반=50%감소
            }
            return 0f;
        }

        /// <summary>
        /// 무적 여부 (무적 제단)
        /// </summary>
        public bool IsInvincible()
        {
            return activeShrineBufss.Exists(b => b.type == ShrineType.Invincibility);
        }

        /// <summary>
        /// 활성 제단 버프 목록
        /// </summary>
        public List<ShrineBuffInstance> GetActiveBuffs()
        {
            return activeShrineBufss;
        }

        /// <summary>
        /// 특정 제단 활성 여부
        /// </summary>
        public bool HasShrineBuff(ShrineType type)
        {
            return activeShrineBufss.Exists(b => b.type == type);
        }

        // === 데이터 ===

        private float GetShrineValue(ShrineType type, bool isPylon)
        {
            // 일반 / 상위(Pylon) 수치
            return type switch
            {
                ShrineType.Protection => isPylon ? 100f : 25f,       // 피해감소 %
                ShrineType.Power => isPylon ? 400f : 50f,            // 데미지 증가 %
                ShrineType.Speed => isPylon ? 75f : 25f,             // 이동속도 증가 %
                ShrineType.Fortune => isPylon ? 100f : 25f,          // 골드/아이템 드롭 증가 %
                ShrineType.Frenzy => isPylon ? 150f : 25f,           // 공격속도 증가 %
                ShrineType.Enlightenment => isPylon ? 100f : 25f,    // 경험치 증가 %
                ShrineType.Channeling => isPylon ? 100f : 75f,       // 쿨다운/자원비용 감소 %
                ShrineType.Conduit => isPylon ? 500f : 200f,         // 번개 데미지
                ShrineType.Invincibility => isPylon ? 100f : 100f,   // 무적
                ShrineType.Empowered => isPylon ? 200f : 100f,       // 전체 강화 %
                _ => 25f
            };
        }

        public static string GetShrineName(ShrineType type)
        {
            return type switch
            {
                ShrineType.Protection => "보호의 제단",
                ShrineType.Power => "힘의 제단",
                ShrineType.Speed => "속도의 제단",
                ShrineType.Fortune => "행운의 제단",
                ShrineType.Frenzy => "광분의 제단",
                ShrineType.Enlightenment => "깨달음의 제단",
                ShrineType.Channeling => "집중의 제단",
                ShrineType.Conduit => "번개의 제단",
                ShrineType.Invincibility => "무적의 제단",
                ShrineType.Empowered => "강화의 제단",
                _ => "알 수 없는 제단"
            };
        }

        public static string GetShrineDescription(ShrineType type, bool isPylon = false)
        {
            string pylonPrefix = isPylon ? "[상위] " : "";
            return type switch
            {
                ShrineType.Protection => $"{pylonPrefix}받는 피해가 크게 감소합니다.",
                ShrineType.Power => $"{pylonPrefix}공격력이 대폭 증가합니다.",
                ShrineType.Speed => $"{pylonPrefix}이동속도가 증가합니다.",
                ShrineType.Fortune => $"{pylonPrefix}골드 및 아이템 드롭률이 증가합니다.",
                ShrineType.Frenzy => $"{pylonPrefix}공격속도가 크게 증가합니다.",
                ShrineType.Enlightenment => $"{pylonPrefix}획득 경험치가 증가합니다.",
                ShrineType.Channeling => $"{pylonPrefix}스킬 쿨다운과 자원 비용이 감소합니다.",
                ShrineType.Conduit => $"{pylonPrefix}주변 적에게 강력한 번개 피해를 줍니다.",
                ShrineType.Invincibility => $"{pylonPrefix}짧은 시간 동안 무적이 됩니다.",
                ShrineType.Empowered => $"{pylonPrefix}모든 능력이 전반적으로 강화됩니다.",
                _ => "알 수 없는 효과"
            };
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    public enum ShrineType
    {
        Protection,     // 보호 - 피해 감소
        Power,          // 힘 - 데미지 증가
        Speed,          // 속도 - 이동속도 증가
        Fortune,        // 행운 - 드롭률 증가
        Frenzy,         // 광분 - 공격속도 증가
        Enlightenment,  // 깨달음 - 경험치 증가
        Channeling,     // 집중 - 쿨다운/비용 감소
        Conduit,        // 번개 - 주변 피해
        Invincibility,  // 무적 - 완전 무적
        Empowered       // 강화 - 전체 강화
    }

    [System.Serializable]
    public struct ShrineBuffInstance
    {
        public ShrineType type;
        public float value;
        public float remainingTime;
        public float totalDuration;
        public bool isPylon;
    }
}
