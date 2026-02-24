using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// MCP 테스트 헬퍼 - IvanMurzak Unity-MCP의 script/execute로 호출
    /// 게임 오브젝트 상태 조회, 시스템 검증, 디버깅 지원
    ///
    /// 사용법 (Roslyn script/execute):
    /// MCPTestHelper.CheckAllSystems()
    /// MCPTestHelper.GetObjectState("Player")
    /// MCPTestHelper.ValidateComponent("Player", "PlayerController")
    /// </summary>
    public static class MCPTestHelper
    {
        #region 시스템 전체 검증

        /// <summary>
        /// 모든 핵심 시스템 싱글톤 존재 여부 확인
        /// </summary>
        [MenuItem("Tools/MCP Test/Check All Systems")]
        public static void CheckAllSystems()
        {
            var systemTypes = new string[]
            {
                "CombatSystem", "SkillManager", "NewSkillLearningSystem",
                "InventoryManager", "EquipmentManager", "WeaponProficiencySystem",
                "DungeonController", "QuestManager", "CraftingSystem",
                "AchievementSystem", "LeaderboardSystem", "SeasonPassSystem",
                "NotificationManager", "ParagonSystem", "RogueliteSystem",
                "LootFilterSystem", "MercenarySystem", "NightmareDungeonSystem",
                "HousingSystem", "BossRushSystem", "ArenaSystem",
                "GuildSystem", "AuctionSystem", "MailSystem",
                "WorldBossSystem", "ExpeditionSystem", "CollectionSystem",
                "ReputationSystem", "DailyRewardSystem", "TradeSystem",
                "PetSystem", "MountSystem", "StorageSystem"
            };

            int found = 0;
            int total = systemTypes.Length;
            var missing = new List<string>();

            foreach (var typeName in systemTypes)
            {
                var type = FindType(typeName);
                if (type != null)
                {
                    var instance = Object.FindFirstObjectByType(type);
                    if (instance != null)
                    {
                        found++;
                        Debug.Log($"[MCP-TEST] OK: {typeName}");
                    }
                    else
                    {
                        missing.Add(typeName + " (type exists, no instance)");
                    }
                }
                else
                {
                    missing.Add(typeName + " (type not found)");
                }
            }

            Debug.Log($"[MCP-TEST] === 시스템 검증 결과: {found}/{total} 활성 ===");
            if (missing.Count > 0)
                Debug.LogWarning($"[MCP-TEST] 미활성: {string.Join(", ", missing)}");
        }

        #endregion

        #region 오브젝트 상태 조회

        /// <summary>
        /// 게임오브젝트 상세 상태 조회
        /// </summary>
        public static string GetObjectState(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
            {
                string msg = $"[MCP-TEST] 오브젝트 '{objectName}' 찾을 수 없음";
                Debug.LogWarning(msg);
                return msg;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MCP-TEST] === {objectName} 상태 ===");
            sb.AppendLine($"  Active: {go.activeSelf}");
            sb.AppendLine($"  Position: {go.transform.position}");
            sb.AppendLine($"  Rotation: {go.transform.eulerAngles}");
            sb.AppendLine($"  Scale: {go.transform.localScale}");
            sb.AppendLine($"  Children: {go.transform.childCount}");
            sb.AppendLine($"  Layer: {LayerMask.LayerToName(go.layer)}");
            sb.AppendLine($"  Tag: {go.tag}");

            var components = go.GetComponents<Component>();
            sb.AppendLine($"  Components ({components.Length}):");
            foreach (var comp in components)
            {
                if (comp == null) continue;
                sb.AppendLine($"    - {comp.GetType().Name}");
            }

            string result = sb.ToString();
            Debug.Log(result);
            return result;
        }

        /// <summary>
        /// 특정 컴포넌트의 public 필드/프로퍼티 값 조회
        /// </summary>
        public static string ValidateComponent(string objectName, string componentName)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return $"[MCP-TEST] 오브젝트 '{objectName}' 없음";

            var comp = go.GetComponents<Component>()
                .FirstOrDefault(c => c != null && c.GetType().Name == componentName);
            if (comp == null) return $"[MCP-TEST] '{objectName}'에 '{componentName}' 없음";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MCP-TEST] === {objectName}/{componentName} ===");

            var type = comp.GetType();

            // Public 필드
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                try
                {
                    var val = f.GetValue(comp);
                    sb.AppendLine($"  [F] {f.Name}: {val}");
                }
                catch { sb.AppendLine($"  [F] {f.Name}: (접근 불가)"); }
            }

            // Public 프로퍼티
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                try
                {
                    var val = p.GetValue(comp);
                    sb.AppendLine($"  [P] {p.Name}: {val}");
                }
                catch { /* skip */ }
            }

            string result = sb.ToString();
            Debug.Log(result);
            return result;
        }

        #endregion

        #region 씬 진단

        /// <summary>
        /// 현재 씬의 모든 루트 오브젝트 목록
        /// </summary>
        [MenuItem("Tools/MCP Test/List Scene Root Objects")]
        public static void ListSceneObjects()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            Debug.Log($"[MCP-TEST] === 씬 '{scene.name}' 루트 오브젝트 ({roots.Length}) ===");
            foreach (var root in roots)
            {
                int childCount = CountChildren(root.transform);
                string active = root.activeSelf ? "O" : "X";
                Debug.Log($"[MCP-TEST]  [{active}] {root.name} (하위: {childCount})");
            }
        }

        /// <summary>
        /// 특정 타입의 모든 컴포넌트 찾기
        /// </summary>
        public static string FindAllOfType(string typeName)
        {
            var type = FindType(typeName);
            if (type == null) return $"[MCP-TEST] 타입 '{typeName}' 찾을 수 없음";

            var objects = Object.FindObjectsByType(type, FindObjectsSortMode.None);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MCP-TEST] === {typeName} 인스턴스 ({objects.Length}) ===");

            foreach (var obj in objects)
            {
                if (obj is Component comp)
                    sb.AppendLine($"  - {comp.gameObject.name} [{(comp.gameObject.activeSelf ? "Active" : "Inactive")}]");
                else
                    sb.AppendLine($"  - {obj.name}");
            }

            string result = sb.ToString();
            Debug.Log(result);
            return result;
        }

        #endregion

        #region 컴파일 에러 사전 검증

        /// <summary>
        /// 런타임 스크립트의 주요 참조 무결성 검증
        /// </summary>
        [MenuItem("Tools/MCP Test/Validate Script References")]
        public static void ValidateScriptReferences()
        {
            int errors = 0;
            int warnings = 0;

            // Resources 폴더 에셋 검증
            var skills = Resources.LoadAll<ScriptableObject>("Skills");
            Debug.Log($"[MCP-TEST] Skills 에셋: {skills.Length}개");
            if (skills.Length < 200) { warnings++; Debug.LogWarning("[MCP-TEST] 스킬 에셋이 200개 미만"); }

            var jobs = Resources.LoadAll<ScriptableObject>("Jobs");
            Debug.Log($"[MCP-TEST] Jobs 에셋: {jobs.Length}개");
            if (jobs.Length < 16) { warnings++; Debug.LogWarning("[MCP-TEST] 직업 에셋이 16개 미만"); }

            var items = Resources.LoadAll<ScriptableObject>("Items");
            int totalItems = 0;
            totalItems += Resources.LoadAll<ScriptableObject>("Items/Weapons").Length;
            totalItems += Resources.LoadAll<ScriptableObject>("Items/Armor").Length;
            totalItems += Resources.LoadAll<ScriptableObject>("Items/Consumables").Length;
            Debug.Log($"[MCP-TEST] Items 에셋: {totalItems}개");

            Debug.Log($"[MCP-TEST] === 참조 검증 완료: {errors} errors, {warnings} warnings ===");
        }

        #endregion

        #region Play Mode 테스트 시퀀스

        /// <summary>
        /// Play Mode에서 주입할 테스트 헬퍼 코드 (Roslyn 용)
        /// script/execute에서 이 메서드가 반환하는 코드를 실행
        /// </summary>
        public static string GetPlayModeTestHelperCode()
        {
            return @"
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RuntimeTestHelper : MonoBehaviour
{
    public static RuntimeTestHelper I;
    private List<string> testLog = new List<string>();

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log(""[TEST] RuntimeTestHelper 주입 완료"");
    }

    public string GetPlayerHP()
    {
        var player = GameObject.FindWithTag(""Player"");
        if (player == null) return ""Player not found"";
        var manager = player.GetComponent<Unity.Template.Multiplayer.NGO.Runtime.PlayerStatsManager>();
        var stats = manager?.CurrentStats;
        return stats != null ? $""HP: {stats.CurrentHP}/{stats.MaxHP}"" : ""No stats"";
    }

    public string GetPlayerPosition()
    {
        var player = GameObject.FindWithTag(""Player"");
        return player != null ? player.transform.position.ToString() : ""Player not found"";
    }

    public string GetAllEnemies()
    {
        var enemies = GameObject.FindGameObjectsWithTag(""Enemy"");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($""Enemies: {enemies.Length}"");
        foreach (var e in enemies)
            sb.AppendLine($""  {e.name} at {e.transform.position}"");
        return sb.ToString();
    }

    public void LogState(string label)
    {
        string entry = $""[{Time.time:F2}] {label}: pos={GetPlayerPosition()}, hp={GetPlayerHP()}"";
        testLog.Add(entry);
        Debug.Log(""[TEST] "" + entry);
    }

    public string GetTestLog()
    {
        return string.Join(""\n"", testLog);
    }
}
";
        }

        #endregion

        #region 유틸리티

        private static System.Type FindType(string typeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null) return type;
            }
            return null;
        }

        private static int CountChildren(Transform t)
        {
            int count = t.childCount;
            for (int i = 0; i < t.childCount; i++)
                count += CountChildren(t.GetChild(i));
            return count;
        }

        #endregion
    }
}
