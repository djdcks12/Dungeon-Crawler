using UnityEngine;
using UnityEditor;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// IvanMurzak Unity-MCP의 script/execute용 테스트 템플릿 모음
    ///
    /// === 사용법 (AI → script/execute) ===
    ///
    /// 패턴 1: 단발 검증 (코루틴 한 방 주입)
    /// -----------------------------------------
    /// public class Test {
    ///     public static object Main() {
    ///         // 여기에 검증 코드
    ///         var go = GameObject.Find("Player");
    ///         Debug.Log($"Player pos: {go.transform.position}");
    ///         return "검증 완료";
    ///     }
    /// }
    ///
    /// 패턴 2: 스텝 바이 스텝 (MCP 도구 체인)
    /// -----------------------------------------
    /// 1. editor/application/set-state → Play Mode 진입
    /// 2. script/execute → 상태 체크 코드
    /// 3. reflection/method-call → 특정 메서드 호출
    /// 4. get-console-logs → 로그 확인
    ///
    /// 패턴 3: 헬퍼 주입 + 반복 호출
    /// -----------------------------------------
    /// 1회: script/execute로 RuntimeTestHelper 주입
    /// 이후: reflection/method-call로 TestHelper.I.GetPlayerHP() 등 호출
    /// </summary>
    public static class MCPTestTemplates
    {
        /// <summary>
        /// 전투 시스템 검증 Roslyn 코드
        /// script/execute에 그대로 전달
        /// </summary>
        [MenuItem("Tools/MCP Test/Print Combat Test Template")]
        public static void PrintCombatTestTemplate()
        {
            Debug.Log(@"
=== 전투 시스템 Roslyn 테스트 ===
script/execute에 다음 코드 전달:

public class Test {
    public static object Main() {
        var systems = new string[] { ""CombatSystem"", ""SkillManager"", ""WeaponProficiencySystem"" };
        var sb = new System.Text.StringBuilder();
        foreach (var s in systems) {
            var type = System.Type.GetType($""Unity.Template.Multiplayer.NGO.Runtime.{s}"");
            var instance = type != null ? UnityEngine.Object.FindFirstObjectByType(type) : null;
            sb.AppendLine($""{s}: {(instance != null ? ""OK"" : ""MISSING"")}"");
        }
        UnityEngine.Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
");
        }

        /// <summary>
        /// 스킬 시스템 검증 Roslyn 코드
        /// </summary>
        [MenuItem("Tools/MCP Test/Print Skill Test Template")]
        public static void PrintSkillTestTemplate()
        {
            Debug.Log(@"
=== 스킬 시스템 Roslyn 테스트 ===
script/execute에 다음 코드 전달:

public class Test {
    public static object Main() {
        var skills = UnityEngine.Resources.LoadAll<UnityEngine.ScriptableObject>(""Skills"");
        var jobs = UnityEngine.Resources.LoadAll<UnityEngine.ScriptableObject>(""Jobs"");
        string result = $""Skills: {skills.Length}, Jobs: {jobs.Length}"";
        UnityEngine.Debug.Log($""[TEST] {result}"");
        return result;
    }
}
");
        }

        /// <summary>
        /// 네트워크 동기화 검증 (Play Mode 필요)
        /// </summary>
        [MenuItem("Tools/MCP Test/Print Network Test Template")]
        public static void PrintNetworkTestTemplate()
        {
            Debug.Log(@"
=== 네트워크 테스트 Roslyn 코드 ===
(Play Mode에서 실행)

public class Test {
    public static object Main() {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) return ""NetworkManager not found"";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($""IsServer: {nm.IsServer}"");
        sb.AppendLine($""IsClient: {nm.IsClient}"");
        sb.AppendLine($""IsHost: {nm.IsHost}"");
        sb.AppendLine($""Connected: {nm.IsConnectedClient}"");
        sb.AppendLine($""Clients: {nm.ConnectedClientsList?.Count ?? 0}"");
        UnityEngine.Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
");
        }
    }
}
