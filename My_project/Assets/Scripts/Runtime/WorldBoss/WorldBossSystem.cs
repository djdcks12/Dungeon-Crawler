using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;
using System.Linq;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 월드보스 시스템 - 서버 권위적
    /// 서버 전체 플레이어 참여, 기여도 기반 보상
    /// </summary>
    public class WorldBossSystem : NetworkBehaviour
    {
        public static WorldBossSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float contributionCheckInterval = 1f;

        // 데이터 캐시
        private Dictionary<string, WorldBossData> bossDatabase = new Dictionary<string, WorldBossData>();
        private List<WorldBossData> bossRotation = new List<WorldBossData>();

        // 서버: 현재 월드보스 상태
        private WorldBossData currentBossData;
        private long currentBossHP;
        private int currentPhase = 1;
        private bool isBossActive;
        private float bossSpawnTime;
        private float nextSpawnTime;
        private int rotationIndex;

        // 서버: 기여도 추적
        private Dictionary<ulong, long> playerDamageContribution = new Dictionary<ulong, long>();
        private Dictionary<ulong, int> playerHitCount = new Dictionary<ulong, int>();

        // 로컬
        private bool localBossActive;
        private string localBossId;
        private string localBossName;
        private long localBossMaxHP;
        private long localBossCurrentHP;
        private int localBossPhase;
        private long localMyContribution;

        // 이벤트
        public System.Action<string, string> OnBossSpawned;   // bossId, bossName
        public System.Action<string> OnBossDefeated;           // bossId
        public System.Action<string> OnBossDespawned;          // bossId (시간 초과)
        public System.Action<long, long> OnBossHPChanged;      // currentHP, maxHP
        public System.Action<int> OnPhaseChanged;              // phase

        // 접근자
        public bool IsBossActive => localBossActive;
        public string CurrentBossName => localBossName;
        public long BossMaxHP => localBossMaxHP;
        public long BossCurrentHP => localBossCurrentHP;
        public int BossPhase => localBossPhase;
        public long MyContribution => localMyContribution;
        public float HPPercent => localBossMaxHP > 0 ? (float)localBossCurrentHP / localBossMaxHP : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LoadBossData();

            if (IsServer)
            {
                nextSpawnTime = Time.time + 120f; // 첫 스폰: 2분 후
            }
        }

        private void LoadBossData()
        {
            var bosses = Resources.LoadAll<WorldBossData>("ScriptableObjects/WorldBoss");
            foreach (var b in bosses)
            {
                if (!string.IsNullOrEmpty(b.BossId))
                {
                    bossDatabase[b.BossId] = b;
                    bossRotation.Add(b);
                }
            }
            Debug.Log($"[WorldBoss] {bossDatabase.Count}개 월드보스 데이터 로드됨");
        }

        private void Update()
        {
            if (!IsServer) return;

            if (isBossActive)
            {
                // 시간 초과 체크
                if (Time.time - bossSpawnTime > currentBossData.DespawnTimeout)
                {
                    DespawnBoss();
                }
            }
            else
            {
                // 스폰 타이머
                if (bossRotation.Count > 0 && Time.time >= nextSpawnTime)
                {
                    SpawnNextBoss();
                }
                // 예고 알림
                else if (bossRotation.Count > 0 && Time.time >= nextSpawnTime - 60f
                    && Time.time < nextSpawnTime - 59f)
                {
                    var nextBoss = bossRotation[rotationIndex % bossRotation.Count];
                    AnnounceUpcomingBossClientRpc(nextBoss.BossName, 60);
                }
            }
        }

        #region 보스 스폰/디스폰

        private void SpawnNextBoss()
        {
            if (bossRotation.Count == 0) return;

            currentBossData = bossRotation[rotationIndex % bossRotation.Count];
            rotationIndex++;
            currentBossHP = currentBossData.MaxHP;
            currentPhase = 1;
            isBossActive = true;
            bossSpawnTime = Time.time;
            playerDamageContribution.Clear();
            playerHitCount.Clear();

            NotifyBossSpawnedClientRpc(
                currentBossData.BossId,
                currentBossData.BossName,
                currentBossData.MaxHP,
                currentBossData.Level
            );
        }

        private void DespawnBoss()
        {
            isBossActive = false;
            string bossId = currentBossData.BossId;
            string bossName = currentBossData.BossName;
            nextSpawnTime = Time.time + currentBossData.SpawnInterval;

            NotifyBossDespawnedClientRpc(bossId, bossName);
            currentBossData = null;
        }

        #endregion

        #region 데미지 처리

        /// <summary>
        /// 월드보스에게 데미지 (서버에서 호출)
        /// </summary>
        public void DealDamageToBoss(ulong attackerClientId, long damage)
        {
            if (!IsServer || !isBossActive || currentBossData == null) return;
            if (damage <= 0) return;

            // 기여도 기록
            if (!playerDamageContribution.ContainsKey(attackerClientId))
                playerDamageContribution[attackerClientId] = 0;
            playerDamageContribution[attackerClientId] += damage;

            if (!playerHitCount.ContainsKey(attackerClientId))
                playerHitCount[attackerClientId] = 0;
            playerHitCount[attackerClientId]++;

            // HP 감소
            currentBossHP -= damage;
            if (currentBossHP < 0) currentBossHP = 0;

            // 페이즈 체크
            float hpPercent = currentBossData.MaxHP > 0 ? (float)currentBossHP / currentBossData.MaxHP : 0f;
            int newPhase = 1;
            if (hpPercent <= currentBossData.EnrageThreshold)
                newPhase = 4; // Enrage
            else if (hpPercent <= currentBossData.Phase3Threshold)
                newPhase = 3;
            else if (hpPercent <= currentBossData.Phase2Threshold)
                newPhase = 2;

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                NotifyPhaseChangedClientRpc(currentPhase);
            }

            // HP 업데이트 브로드캐스트
            BroadcastBossHPClientRpc(currentBossHP, currentBossData.MaxHP);

            // 개인 기여도 업데이트
            UpdateContributionClientRpc(playerDamageContribution[attackerClientId], attackerClientId);

            // 처치 확인
            if (currentBossHP <= 0)
            {
                DefeatBoss();
            }
        }

        /// <summary>
        /// ServerRpc: 플레이어가 보스 공격
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AttackBossServerRpc(long damage, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            DealDamageToBoss(clientId, damage);
        }

        #endregion

        #region 보스 처치 & 보상

        private void DefeatBoss()
        {
            isBossActive = false;
            string bossId = currentBossData.BossId;
            string bossName = currentBossData.BossName;

            // 보상 분배
            DistributeRewards();

            // 다음 스폰 예약
            nextSpawnTime = Time.time + currentBossData.SpawnInterval;

            NotifyBossDefeatedClientRpc(bossId, bossName);
            currentBossData = null;
        }

        private void DistributeRewards()
        {
            if (playerDamageContribution.Count == 0) return;

            long totalDamage = playerDamageContribution.Values.Sum();
            if (totalDamage <= 0) return;

            // 기여도 순위 정렬
            var sortedContributors = playerDamageContribution
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            int rank = 1;
            foreach (var kvp in sortedContributors)
            {
                ulong clientId = kvp.Key;
                long contribution = kvp.Value;
                float contributionPercent = (float)contribution / totalDamage;

                // 기여도 기반 보상 배율
                float rewardMultiplier = Mathf.Max(0.1f, contributionPercent * 3f); // 최소 10%
                if (rank <= 3) rewardMultiplier += 0.5f * (4 - rank); // 상위 3명 추가 보상

                int expReward = Mathf.RoundToInt(currentBossData.BaseExpReward * rewardMultiplier);
                int goldReward = Mathf.RoundToInt(currentBossData.BaseGoldReward * rewardMultiplier);

                // 골드/경험치 지급
                var statsData = GetPlayerStatsData(clientId);
                if (statsData != null)
                {
                    statsData.ChangeGold(goldReward);
                    statsData.AddExperience(expReward);
                }

                // 아이템 드롭 (상위 기여자에게)
                if (rank <= 5 && currentBossData.GuaranteedDrops != null && currentBossData.GuaranteedDrops.Length > 0)
                {
                    // 우편으로 보상 전송
                    if (MailSystem.Instance != null)
                    {
                        string itemId = currentBossData.GuaranteedDrops[
                            Random.Range(0, currentBossData.GuaranteedDrops.Length)];

                        var attachment = new MailAttachment
                        {
                            gold = goldReward,
                            itemId = itemId,
                            quantity = 1,
                            enhanceLevel = 0
                        };

                        if (MailSystem.Instance != null)
                            MailSystem.Instance.SendSystemMail(clientId,
                                $"[월드보스] {currentBossData.BossName} 처치 보상",
                                $"축하합니다! {rank}위로 보스를 처치했습니다.\n기여도: {contributionPercent:P1}",
                                MailType.SystemReward, attachment, true);
                    }
                }

                // 레어 드롭 체크 (기여도에 비례)
                if (currentBossData.RareDrops != null && currentBossData.RareDrops.Length > 0)
                {
                    float adjustedChance = currentBossData.RareDropChance * (1f + contributionPercent);
                    if (Random.value < adjustedChance)
                    {
                        string rareItem = currentBossData.RareDrops[
                            Random.Range(0, currentBossData.RareDrops.Length)];

                        if (MailSystem.Instance != null)
                        {
                            var rareAttachment = new MailAttachment
                            {
                                itemId = rareItem,
                                quantity = 1
                            };
                            MailSystem.Instance.SendSystemMail(clientId,
                                $"[월드보스] 레어 드롭!",
                                $"{currentBossData.BossName}에서 희귀 아이템을 획득했습니다!",
                                MailType.SystemReward, rareAttachment, true);
                        }
                    }
                }

                // 클라이언트에 보상 알림
                NotifyRewardClientRpc(rank, expReward, goldReward, contributionPercent, clientId);
                rank++;
            }
        }

        #endregion

        #region ClientRPCs

        [ClientRpc]
        private void NotifyBossSpawnedClientRpc(string bossId, string bossName, long maxHP, int level)
        {
            localBossActive = true;
            localBossId = bossId;
            localBossName = bossName;
            localBossMaxHP = maxHP;
            localBossCurrentHP = maxHP;
            localBossPhase = 1;
            localMyContribution = 0;
            OnBossSpawned?.Invoke(bossId, bossName);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"월드보스 출현: {bossName} (Lv.{level})!", NotificationType.Warning);
        }

        [ClientRpc]
        private void NotifyBossDefeatedClientRpc(string bossId, string bossName)
        {
            localBossActive = false;
            OnBossDefeated?.Invoke(bossId);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"월드보스 {bossName} 처치 완료!", NotificationType.System);
        }

        [ClientRpc]
        private void NotifyBossDespawnedClientRpc(string bossId, string bossName)
        {
            localBossActive = false;
            OnBossDespawned?.Invoke(bossId);

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"월드보스 {bossName}이(가) 사라졌습니다.", NotificationType.Warning);
        }

        [ClientRpc]
        private void BroadcastBossHPClientRpc(long currentHP, long maxHP)
        {
            localBossCurrentHP = currentHP;
            localBossMaxHP = maxHP;
            OnBossHPChanged?.Invoke(currentHP, maxHP);
        }

        [ClientRpc]
        private void NotifyPhaseChangedClientRpc(int phase)
        {
            localBossPhase = phase;
            OnPhaseChanged?.Invoke(phase);

            string phaseMsg = phase switch
            {
                2 => "보스가 분노하기 시작합니다!",
                3 => "보스가 강력한 공격을 시전합니다!",
                4 => "보스가 광폭화했습니다!!",
                _ => ""
            };

            if (!string.IsNullOrEmpty(phaseMsg))
            {
                var notif = NotificationManager.Instance;
                if (notif != null)
                    notif.ShowNotification(phaseMsg, NotificationType.Warning);
            }
        }

        [ClientRpc]
        private void UpdateContributionClientRpc(long contribution, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
            localMyContribution = contribution;
        }

        [ClientRpc]
        private void NotifyRewardClientRpc(int rank, int exp, int gold, float percent, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification(
                    $"월드보스 보상 ({rank}위, 기여도 {percent:P1}): EXP+{exp}, Gold+{gold}",
                    NotificationType.ItemRare);
        }

        [ClientRpc]
        private void AnnounceUpcomingBossClientRpc(string bossName, int seconds)
        {
            var notif = NotificationManager.Instance;
            if (notif != null)
                notif.ShowNotification($"월드보스 {bossName} {seconds}초 후 출현!", NotificationType.Warning);
        }

        #endregion

        #region Utility

        private PlayerStatsData GetPlayerStatsData(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStatsManager>()?.CurrentStats;
        }

        /// <summary>
        /// 기여도 순위 조회
        /// </summary>
        public List<KeyValuePair<ulong, long>> GetContributionRanking()
        {
            return playerDamageContribution
                .OrderByDescending(kvp => kvp.Value)
                .ToList();
        }

        #endregion

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                OnBossSpawned = null;
                OnBossDefeated = null;
                OnBossDespawned = null;
                OnBossHPChanged = null;
                OnPhaseChanged = null;
                Instance = null;
            }
            base.OnDestroy();
        }
    }
}
