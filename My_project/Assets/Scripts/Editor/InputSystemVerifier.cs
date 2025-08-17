using UnityEngine;
using UnityEditor;

namespace Unity.Template.Multiplayer.NGO.Runtime.Editor
{
    /// <summary>
    /// Input System 설정이 올바르게 적용되었는지 확인하는 스크립트
    /// </summary>
    public static class InputSystemVerifier
    {
        [MenuItem("Dungeon Crawler/Verify Input System Fix")]
        public static void VerifyInputSystemFix()
        {
            Debug.Log("=== Input System 설정 확인 ===");
            
            // 1. ProjectSettings.asset 파일 직접 확인
            string projectSettingsPath = "ProjectSettings/ProjectSettings.asset";
            if (System.IO.File.Exists(projectSettingsPath))
            {
                string content = System.IO.File.ReadAllText(projectSettingsPath);
                if (content.Contains("activeInputHandler: 0"))
                {
                    Debug.Log("✅ ProjectSettings.asset: activeInputHandler = 0 (올바름)");
                }
                else if (content.Contains("activeInputHandler: 1"))
                {
                    Debug.LogError("❌ ProjectSettings.asset: activeInputHandler = 1 (잘못됨)");
                }
                else
                {
                    Debug.LogWarning("⚠️ ProjectSettings.asset: activeInputHandler 설정을 찾을 수 없음");
                }
            }
            
            // 2. SerializedObject로 확인
            try
            {
                var projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
                var serializedObject = new SerializedObject(projectSettings);
                var activeInputHandler = serializedObject.FindProperty("activeInputHandler");
                
                if (activeInputHandler != null)
                {
                    int value = activeInputHandler.intValue;
                    if (value == 0)
                    {
                        Debug.Log($"✅ Unity API: activeInputHandler = {value} (Input Manager - 올바름)");
                    }
                    else
                    {
                        Debug.LogError($"❌ Unity API: activeInputHandler = {value} (Input System Package - 잘못됨)");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Unity API: activeInputHandler 프로퍼티를 찾을 수 없음");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Unity API 확인 실패: {e.Message}");
            }
            
            // 3. Input System Package 존재 확인
            bool hasInputSystemPackage = System.Type.GetType("UnityEngine.InputSystem.InputSystem") != null;
            if (hasInputSystemPackage)
            {
                Debug.LogWarning("⚠️ Input System Package가 여전히 프로젝트에 설치되어 있습니다.");
                Debug.LogWarning("   완전한 해결을 위해 Package Manager에서 Input System을 제거하는 것을 고려하세요.");
            }
            else
            {
                Debug.Log("✅ Input System Package가 프로젝트에 설치되어 있지 않습니다.");
            }
            
            // 4. 최종 결과
            Debug.Log("=== 최종 확인 ===");
            
            // 실제 Input.GetKeyDown 테스트 (에디터에서만)
            #if UNITY_EDITOR
            try
            {
                bool testResult = Input.GetKeyDown(KeyCode.Escape);
                Debug.Log("✅ Input.GetKeyDown 테스트 성공! Input System이 Legacy로 설정되었습니다.");
                
                EditorUtility.DisplayDialog(
                    "Input System 확인 완료",
                    "✅ Input System이 올바르게 Legacy Input으로 설정되었습니다!\n\n이제 Play 모드에서 에러가 발생하지 않을 것입니다.",
                    "확인"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Input.GetKeyDown 테스트 실패: {e.Message}");
                Debug.LogError("Input System이 여전히 활성화되어 있거나 Unity 재시작이 필요할 수 있습니다.");
                
                EditorUtility.DisplayDialog(
                    "Input System 확인 실패",
                    $"❌ Input System 설정이 아직 적용되지 않았습니다.\n\n에러: {e.Message}\n\nUnity Editor를 재시작해주세요.",
                    "확인"
                );
            }
            #endif
        }
        
        [MenuItem("Dungeon Crawler/Test Legacy Input")]
        public static void TestLegacyInput()
        {
            Debug.Log("=== Legacy Input 테스트 ===");
            
            try
            {
                // 다양한 Input 함수들 테스트
                bool keyDown = Input.GetKeyDown(KeyCode.Space);
                bool keyUp = Input.GetKeyUp(KeyCode.Space);
                bool key = Input.GetKey(KeyCode.Space);
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                bool mouseButton = Input.GetMouseButtonDown(0);
                
                Debug.Log($"✅ 모든 Legacy Input 함수가 정상 작동합니다!");
                Debug.Log($"   GetKeyDown: {keyDown}, GetAxis: ({horizontal}, {vertical}), Mouse: {mouseButton}");
                
                EditorUtility.DisplayDialog(
                    "Legacy Input 테스트 성공",
                    "✅ 모든 Legacy Input 함수가 정상적으로 작동합니다!\n\n이제 게임을 안전하게 실행할 수 있습니다.",
                    "확인"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Legacy Input 테스트 실패: {e.Message}");
                
                EditorUtility.DisplayDialog(
                    "Legacy Input 테스트 실패",
                    $"❌ Legacy Input이 아직 작동하지 않습니다.\n\n에러: {e.Message}\n\nUnity Editor를 재시작하거나 'FORCE Fix Input System (Direct)' 메뉴를 사용해주세요.",
                    "확인"
                );
            }
        }
    }
}