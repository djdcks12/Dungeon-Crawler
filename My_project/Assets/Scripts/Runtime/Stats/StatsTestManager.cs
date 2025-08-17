using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 스탯 시스템 테스트 매니저
    /// 개발 및 디버깅용 스탯 테스트 기능 제공
    /// </summary>
    public class StatsTestManager : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool enableDebugUI = true;
        [SerializeField] private KeyCode testMenuKey = KeyCode.F2;
        [SerializeField] private KeyCode addExpKey = KeyCode.F3;
        [SerializeField] private KeyCode damageTestKey = KeyCode.F4;
        [SerializeField] private KeyCode healTestKey = KeyCode.F5;
        
        [Header("Test Values")]
        [SerializeField] private long testExpAmount = 100;
        [SerializeField] private float testDamageAmount = 25f;
        [SerializeField] private float testHealAmount = 50f;
        
        private bool showTestPanel = false;
        private PlayerStatsManager localStatsManager;
        private Vector2 scrollPosition;
        
        private void Start()
        {
            FindLocalPlayerStatsManager();
        }
        
        private void Update()
        {
            HandleTestInputs();
        }
        
        private void HandleTestInputs()
        {
            if (Input.GetKeyDown(testMenuKey))
            {
                showTestPanel = !showTestPanel;
            }
            
            if (Input.GetKeyDown(addExpKey))
            {
                AddTestExperience();
            }
            
            if (Input.GetKeyDown(damageTestKey))
            {
                TestDamage();
            }
            
            if (Input.GetKeyDown(healTestKey))
            {
                TestHeal();
            }
        }
        
        private void OnGUI()
        {
            if (!enableDebugUI || !showTestPanel) return;
            
            // 메인 패널
            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            GUILayout.Box("Stats System Test Panel", GUILayout.Width(390), GUILayout.Height(590));
            
            GUILayout.BeginArea(new Rect(10, 30, 380, 550));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            // 플레이어 정보
            DrawPlayerInfo();
            GUILayout.Space(10);
            
            // 스탯 정보
            DrawStatsInfo();
            GUILayout.Space(10);
            
            // 테스트 버튼들
            DrawTestButtons();
            GUILayout.Space(10);
            
            // 스탯 증가 테스트
            DrawStatIncrementTest();
            GUILayout.Space(10);
            
            // 밸런싱 정보
            DrawBalancingInfo();
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.EndArea();
        }
        
        private void DrawPlayerInfo()
        {
            GUILayout.Label("=== Player Information ===", GUI.skin.box);
            
            if (localStatsManager == null || localStatsManager.CurrentStats == null)
            {
                GUILayout.Label("No player stats found!");
                if (GUILayout.Button("Refresh Player"))
                {
                    FindLocalPlayerStatsManager();
                }
                return;
            }
            
            var stats = localStatsManager.CurrentStats;
            
            GUILayout.Label($"Level: {stats.CurrentLevel}");
            GUILayout.Label($"EXP: {stats.CurrentExp:N0} / {stats.ExpToNextLevel:N0}");
            // 스탯 포인트 시스템 제거됨 (종족별 고정 성장)
            // GUILayout.Label($"Available Points: {stats.AvailableStatPoints}");
            GUILayout.Label($"HP: {stats.CurrentHP:F1} / {stats.MaxHP:F1}");
            GUILayout.Label($"MP: {stats.CurrentMP:F1} / {stats.MaxMP:F1}");
            
            // 체력/마나 바
            DrawHealthBar(stats.CurrentHP / stats.MaxHP);
            DrawManaBar(stats.CurrentMP / stats.MaxMP);
        }
        
        private void DrawStatsInfo()
        {
            GUILayout.Label("=== Primary Stats ===", GUI.skin.box);
            
            if (localStatsManager?.CurrentStats == null) return;
            
            var stats = localStatsManager.CurrentStats;
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"STR: {stats.TotalSTR:F0}", GUILayout.Width(80));
            GUILayout.Label($"AGI: {stats.TotalAGI:F0}", GUILayout.Width(80));
            GUILayout.Label($"VIT: {stats.TotalVIT:F0}", GUILayout.Width(80));
            GUILayout.Label($"INT: {stats.TotalINT:F0}", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"DEF: {stats.TotalDEF:F0}", GUILayout.Width(80));
            GUILayout.Label($"MDEF: {stats.TotalMDEF:F0}", GUILayout.Width(80));
            GUILayout.Label($"LUK: {stats.TotalLUK:F0}", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            GUILayout.Label("=== Derived Stats ===", GUI.skin.box);
            
            GUILayout.Label($"Attack Damage: {stats.AttackDamage:F1}");
            GUILayout.Label($"Magic Damage: {stats.MagicDamage:F1}");
            GUILayout.Label($"Move Speed: {stats.MoveSpeed:F2}");
            GUILayout.Label($"Attack Speed: {stats.AttackSpeed:F2}");
            GUILayout.Label($"Critical Chance: {stats.CriticalChance:P2}");
            GUILayout.Label($"Critical Damage: {stats.CriticalDamage:P0}");
        }
        
        private void DrawTestButtons()
        {
            GUILayout.Label("=== Quick Tests ===", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Add {testExpAmount} EXP"))
            {
                AddTestExperience();
            }
            if (GUILayout.Button("Level Up"))
            {
                LevelUpTest();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Take {testDamageAmount} DMG"))
            {
                TestDamage();
            }
            if (GUILayout.Button($"Heal {testHealAmount} HP"))
            {
                TestHeal();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Stats"))
            {
                ResetStatsTest();
            }
            if (GUILayout.Button("Max Level"))
            {
                MaxLevelTest();
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Run Full Test Suite"))
            {
                StartCoroutine(RunFullTestSuite());
            }
        }
        
        private void DrawStatIncrementTest()
        {
            GUILayout.Label("=== Race-Based Auto Growth ===", GUI.skin.box);
            
            if (localStatsManager?.CurrentStats == null) return;
            
            var stats = localStatsManager.CurrentStats;
            
            // 스탯 포인트 수동 분배 시스템 제거됨
            // 종족별 고정 성장으로 레벨업 시 자동 스탯 증가
            GUILayout.Label("Stats grow automatically based on race when leveling up");
            GUILayout.Label($"Current Race: {stats.CharacterRace}");
            
            if (GUILayout.Button("Add 100 EXP (Test Level Up)"))
            {
                localStatsManager.AddExperience(100);
            }
        }
        
        private void DrawBalancingInfo()
        {
            GUILayout.Label("=== Balancing Information ===", GUI.skin.box);
            
            if (localStatsManager?.CurrentStats == null) return;
            
            var stats = localStatsManager.CurrentStats;
            
            // DPS 계산
            float dps = stats.AttackDamage * stats.AttackSpeed;
            float critDPS = dps * (1f + stats.CriticalChance * (stats.CriticalDamage - 1f));
            
            GUILayout.Label($"Base DPS: {dps:F2}");
            GUILayout.Label($"Crit DPS: {critDPS:F2}");
            
            // 생존력 지수
            float survivability = stats.MaxHP * (1f + stats.TotalDEF * 0.01f);
            GUILayout.Label($"Survivability: {survivability:F0}");
            
            // 효율성 평가
            float statTotal = stats.TotalSTR + stats.TotalAGI + stats.TotalVIT + stats.TotalINT + 
                             stats.TotalDEF + stats.TotalMDEF + stats.TotalLUK;
            GUILayout.Label($"Total Stats: {statTotal:F0}");
            
            // 예상 레벨 (70은 기본 스탯 합계)
            int estimatedLevel = Mathf.RoundToInt((statTotal - 70f) / 5f) + 1;
            GUILayout.Label($"Est. Min Level: {estimatedLevel}");
        }
        
        private void DrawHealthBar(float percentage)
        {
            Rect rect = GUILayoutUtility.GetRect(200, 10);
            GUI.Box(rect, "");
            
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * percentage, rect.height);
            GUI.color = Color.Lerp(Color.red, Color.green, percentage);
            GUI.Box(fillRect, "");
            GUI.color = Color.white;
        }
        
        private void DrawManaBar(float percentage)
        {
            Rect rect = GUILayoutUtility.GetRect(200, 10);
            GUI.Box(rect, "");
            
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * percentage, rect.height);
            GUI.color = Color.blue;
            GUI.Box(fillRect, "");
            GUI.color = Color.white;
        }
        
        private void FindLocalPlayerStatsManager()
        {
            var playerControllers = FindObjectsOfType<PlayerController>();
            foreach (var controller in playerControllers)
            {
                var networkBehaviour = controller.GetComponent<NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.IsLocalPlayer)
                {
                    localStatsManager = controller.GetComponent<PlayerStatsManager>();
                    Debug.Log("Local player stats manager found");
                    return;
                }
            }
            
            Debug.LogWarning("Local player stats manager not found");
        }
        
        // 테스트 메서드들
        private void AddTestExperience()
        {
            if (localStatsManager != null)
            {
                localStatsManager.AddExperienceServerRpc(testExpAmount);
                Debug.Log($"Added {testExpAmount} experience");
            }
        }
        
        private void LevelUpTest()
        {
            if (localStatsManager?.CurrentStats != null)
            {
                var stats = localStatsManager.CurrentStats;
                long expNeeded = stats.ExpToNextLevel - stats.CurrentExp;
                localStatsManager.AddExperienceServerRpc(expNeeded);
                Debug.Log("Leveled up player");
            }
        }
        
        private void TestDamage()
        {
            if (localStatsManager != null)
            {
                localStatsManager.TakeDamage(testDamageAmount);
                Debug.Log($"Applied {testDamageAmount} damage");
            }
        }
        
        private void TestHeal()
        {
            if (localStatsManager != null)
            {
                localStatsManager.Heal(testHealAmount);
                Debug.Log($"Healed {testHealAmount} HP");
            }
        }
        
        private void ResetStatsTest()
        {
            if (localStatsManager != null)
            {
                // 종족별 고정 성장 시스템에서는 스탯 리셋 대신 영혼 보너스만 리셋 가능
                localStatsManager.ResetSoulBonusStats();
                Debug.Log("Soul bonus stats reset - base race stats remain unchanged");
            }
        }
        
        private void MaxLevelTest()
        {
            if (localStatsManager?.CurrentStats != null)
            {
                localStatsManager.AddExperienceServerRpc(999999999);
                Debug.Log("Max level test");
            }
        }
        
        // 전체 테스트 스위트
        private IEnumerator RunFullTestSuite()
        {
            Debug.Log("=== Starting Full Stats Test Suite ===");
            
            if (localStatsManager == null)
            {
                Debug.LogError("No stats manager found for testing");
                yield break;
            }
            
            // 1. 종족별 성장 테스트
            Debug.Log("Testing race-based auto growth...");
            Debug.Log($"Current race: {localStatsManager.CurrentStats.CharacterRace}");
            Debug.Log("Stats will grow automatically on level up based on race data");
            yield return new WaitForSeconds(0.5f);
            
            // 2. 경험치 테스트
            Debug.Log("Testing experience gain...");
            for (int i = 0; i < 5; i++)
            {
                localStatsManager.AddExperienceServerRpc(50);
                yield return new WaitForSeconds(0.2f);
            }
            
            // 3. 데미지/힐링 테스트
            Debug.Log("Testing damage and healing...");
            localStatsManager.TakeDamage(30f);
            yield return new WaitForSeconds(0.5f);
            localStatsManager.Heal(20f);
            yield return new WaitForSeconds(0.5f);
            
            // 4. 성능 테스트 (스탯 계산 속도)
            Debug.Log("Testing calculation performance...");
            float startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < 1000; i++)
            {
                localStatsManager.CurrentStats.RecalculateStats();
            }
            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"1000 stat calculations took: {(endTime - startTime) * 1000f:F2}ms");
            
            Debug.Log("=== Test Suite Complete ===");
        }
        
        // 밸런싱 분석
        public void AnalyzeBalance()
        {
            if (localStatsManager?.CurrentStats == null) return;
            
            var stats = localStatsManager.CurrentStats;
            
            Debug.Log("=== Balance Analysis ===");
            
            // 스탯 분포 분석
            float[] statValues = {
                stats.TotalSTR, stats.TotalAGI, stats.TotalVIT, stats.TotalINT,
                stats.TotalDEF, stats.TotalMDEF, stats.TotalLUK
            };
            
            float total = 0f;
            float max = 0f;
            float min = float.MaxValue;
            
            foreach (float value in statValues)
            {
                total += value;
                max = Mathf.Max(max, value);
                min = Mathf.Min(min, value);
            }
            
            float average = total / statValues.Length;
            float variance = 0f;
            
            foreach (float value in statValues)
            {
                variance += (value - average) * (value - average);
            }
            variance /= statValues.Length;
            
            Debug.Log($"Stat Distribution - Avg: {average:F1}, Min: {min:F1}, Max: {max:F1}, Variance: {variance:F1}");
            
            // 능력치 효율성
            float dpsPerStat = (stats.AttackDamage * stats.AttackSpeed) / (stats.TotalSTR + stats.TotalAGI);
            float hpPerStat = stats.MaxHP / stats.TotalVIT;
            
            Debug.Log($"DPS per STR+AGI: {dpsPerStat:F2}");
            Debug.Log($"HP per VIT: {hpPerStat:F2}");
        }
    }
}