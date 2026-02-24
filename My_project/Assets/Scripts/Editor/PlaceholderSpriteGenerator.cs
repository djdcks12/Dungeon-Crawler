using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 플레이스홀더 스프라이트 자동 생성 에디터 도구
    /// 종족/몬스터/NPC별 고유 색상 32x32 스프라이트 생성
    /// </summary>
    public class PlaceholderSpriteGenerator
    {
        private static readonly int SpriteSize = 32;

        // 플레이어 종족별 색상
        private static readonly Color ElfColor = new Color(0.5f, 0.9f, 0.4f);       // 연두색
        private static readonly Color BeastColor = new Color(0.6f, 0.4f, 0.2f);     // 갈색
        private static readonly Color MachinaColor = new Color(0.75f, 0.75f, 0.8f); // 은색

        // 몬스터 종족별 색상
        private static readonly Color OrcColor = new Color(0.2f, 0.6f, 0.2f);       // 녹색
        private static readonly Color UndeadColor = new Color(0.5f, 0.2f, 0.6f);    // 보라색
        private static readonly Color DemonColor = new Color(0.8f, 0.15f, 0.15f);   // 적색
        private static readonly Color DragonColor = new Color(0.9f, 0.75f, 0.1f);   // 금색
        private static readonly Color ElementalColor = new Color(0.1f, 0.8f, 0.8f); // 청록
        private static readonly Color ConstructColor = new Color(0.5f, 0.5f, 0.5f); // 회색
        private static readonly Color BeastMonsterColor = new Color(0.65f, 0.45f, 0.25f); // 갈색

        // NPC 색상
        private static readonly Color SkillMasterColor = new Color(0.2f, 0.4f, 0.9f);   // 파란색
        private static readonly Color MerchantColor = new Color(0.9f, 0.75f, 0.1f);     // 금색
        private static readonly Color DungeonPortalColor = new Color(0.6f, 0.1f, 0.8f); // 보라색

        [MenuItem("Tools/Sprites/Generate All Placeholder Sprites")]
        public static void GenerateAllPlaceholderSprites()
        {
            // 플레이어 종족
            GeneratePlayerSprites("Elf", ElfColor);
            GeneratePlayerSprites("Beast", BeastColor);
            GeneratePlayerSprites("Machina", MachinaColor);

            // 몬스터 종족
            GenerateMonsterSprites("Orc", OrcColor);
            GenerateMonsterSprites("Undead", UndeadColor);
            GenerateMonsterSprites("Demon", DemonColor);
            GenerateMonsterSprites("Dragon", DragonColor);
            GenerateMonsterSprites("Elemental", ElementalColor);
            GenerateMonsterSprites("Construct", ConstructColor);
            GenerateMonsterSprites("Beast", BeastMonsterColor);

            // NPC
            GenerateNPCSprite("SkillMaster", SkillMasterColor);
            GenerateNPCSprite("Merchant", MerchantColor);
            GenerateNPCSprite("DungeonPortal", DungeonPortalColor);
            GenerateNPCSprite("Blacksmith", new Color(0.4f, 0.3f, 0.3f));
            GenerateNPCSprite("QuestNPC", new Color(0.9f, 0.9f, 0.2f));

            AssetDatabase.Refresh();
            Debug.Log("All placeholder sprites generated!");
        }

        [MenuItem("Tools/Sprites/Generate Player Placeholder Sprites")]
        public static void GeneratePlayerPlaceholderSprites()
        {
            GeneratePlayerSprites("Elf", ElfColor);
            GeneratePlayerSprites("Beast", BeastColor);
            GeneratePlayerSprites("Machina", MachinaColor);
            AssetDatabase.Refresh();
            Debug.Log("Player placeholder sprites generated!");
        }

        [MenuItem("Tools/Sprites/Generate Monster Placeholder Sprites")]
        public static void GenerateMonsterPlaceholderSprites()
        {
            GenerateMonsterSprites("Orc", OrcColor);
            GenerateMonsterSprites("Undead", UndeadColor);
            GenerateMonsterSprites("Demon", DemonColor);
            GenerateMonsterSprites("Dragon", DragonColor);
            GenerateMonsterSprites("Elemental", ElementalColor);
            GenerateMonsterSprites("Construct", ConstructColor);
            GenerateMonsterSprites("Beast", BeastMonsterColor);
            AssetDatabase.Refresh();
            Debug.Log("Monster placeholder sprites generated!");
        }

        [MenuItem("Tools/Sprites/Generate NPC Placeholder Sprites")]
        public static void GenerateNPCPlaceholderSprites()
        {
            GenerateNPCSprite("SkillMaster", SkillMasterColor);
            GenerateNPCSprite("Merchant", MerchantColor);
            GenerateNPCSprite("DungeonPortal", DungeonPortalColor);
            GenerateNPCSprite("Blacksmith", new Color(0.4f, 0.3f, 0.3f));
            GenerateNPCSprite("QuestNPC", new Color(0.9f, 0.9f, 0.2f));
            AssetDatabase.Refresh();
            Debug.Log("NPC placeholder sprites generated!");
        }

        /// <summary>
        /// 플레이어 종족 스프라이트 생성 (Player 패턴: _Idle, _Run, _Attack, _Hit, _Death)
        /// </summary>
        private static void GeneratePlayerSprites(string raceName, Color baseColor)
        {
            string basePath = $"Assets/Resources/Sprites/Player/{raceName}";
            EnsureDirectory(basePath);

            // Player 스프라이트 이름 패턴 (기존 Human과 동일)
            SaveSprite(CreateCharacterSprite(baseColor, 1.0f), $"{basePath}/_Idle.png");
            SaveSprite(CreateCharacterSprite(baseColor, 0.85f), $"{basePath}/_Run.png");
            SaveSprite(CreateCharacterSprite(baseColor, 1.2f), $"{basePath}/_Attack.png");
            SaveSprite(CreateCharacterSprite(baseColor, 0.6f), $"{basePath}/_Hit.png");
            SaveSprite(CreateCharacterSprite(baseColor, 0.3f), $"{basePath}/_Death.png");

            Debug.Log($"Player sprites generated: {raceName}");
        }

        /// <summary>
        /// 몬스터 종족 스프라이트 생성 (Monster 패턴: Idle, Run, Attack, Take Hit, Death)
        /// </summary>
        private static void GenerateMonsterSprites(string raceName, Color baseColor)
        {
            string basePath = $"Assets/Resources/Sprites/Monster/{raceName}";
            EnsureDirectory(basePath);

            // Monster 스프라이트 이름 패턴 (기존 Goblin과 동일)
            SaveSprite(CreateMonsterSprite(baseColor, 1.0f), $"{basePath}/Idle.png");
            SaveSprite(CreateMonsterSprite(baseColor, 0.85f), $"{basePath}/Run.png");
            SaveSprite(CreateMonsterSprite(baseColor, 1.2f), $"{basePath}/Attack.png");
            SaveSprite(CreateMonsterSprite(baseColor, 0.6f), $"{basePath}/Take Hit.png");
            SaveSprite(CreateMonsterSprite(baseColor, 0.3f), $"{basePath}/Death.png");

            Debug.Log($"Monster sprites generated: {raceName}");
        }

        /// <summary>
        /// NPC 스프라이트 생성
        /// </summary>
        private static void GenerateNPCSprite(string npcName, Color baseColor)
        {
            string basePath = "Assets/Resources/Sprites/NPC";
            EnsureDirectory(basePath);

            SaveSprite(CreateNPCSprite(baseColor), $"{basePath}/{npcName}.png");
            Debug.Log($"NPC sprite generated: {npcName}");
        }

        /// <summary>
        /// 캐릭터형 스프라이트 생성 (인간형 실루엣)
        /// </summary>
        private static Texture2D CreateCharacterSprite(Color baseColor, float brightnessMultiplier)
        {
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color adjusted = new Color(
                Mathf.Clamp01(baseColor.r * brightnessMultiplier),
                Mathf.Clamp01(baseColor.g * brightnessMultiplier),
                Mathf.Clamp01(baseColor.b * brightnessMultiplier),
                1f
            );

            Color outline = adjusted * 0.5f;
            outline.a = 1f;

            // 투명으로 초기화
            Color[] pixels = new Color[SpriteSize * SpriteSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // 인간형 실루엣 (머리 + 몸통 + 다리)
            // 머리 (원형, 중심 상단)
            DrawCircle(pixels, SpriteSize, 16, 26, 4, adjusted);
            DrawCircle(pixels, SpriteSize, 16, 26, 5, outline, true);

            // 몸통
            FillRect(pixels, SpriteSize, 12, 13, 20, 22, adjusted);
            DrawRect(pixels, SpriteSize, 11, 12, 21, 23, outline);

            // 팔
            FillRect(pixels, SpriteSize, 8, 14, 11, 22, adjusted);
            FillRect(pixels, SpriteSize, 18, 14, 23, 22, adjusted);

            // 다리
            FillRect(pixels, SpriteSize, 12, 4, 15, 13, adjusted);
            FillRect(pixels, SpriteSize, 17, 4, 20, 13, adjusted);

            // 눈
            SetPixelSafe(pixels, SpriteSize, 14, 27, Color.white);
            SetPixelSafe(pixels, SpriteSize, 18, 27, Color.white);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 몬스터형 스프라이트 생성 (덩어리형 실루엣)
        /// </summary>
        private static Texture2D CreateMonsterSprite(Color baseColor, float brightnessMultiplier)
        {
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color adjusted = new Color(
                Mathf.Clamp01(baseColor.r * brightnessMultiplier),
                Mathf.Clamp01(baseColor.g * brightnessMultiplier),
                Mathf.Clamp01(baseColor.b * brightnessMultiplier),
                1f
            );

            Color outline = adjusted * 0.4f;
            outline.a = 1f;
            Color highlight = Color.Lerp(adjusted, Color.white, 0.3f);

            Color[] pixels = new Color[SpriteSize * SpriteSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // 몬스터 몸체 (큰 타원형)
            DrawEllipse(pixels, SpriteSize, 16, 14, 10, 12, adjusted);
            DrawEllipse(pixels, SpriteSize, 16, 14, 11, 13, outline, true);

            // 눈 (빨간색)
            SetPixelSafe(pixels, SpriteSize, 12, 18, Color.red);
            SetPixelSafe(pixels, SpriteSize, 13, 18, Color.red);
            SetPixelSafe(pixels, SpriteSize, 19, 18, Color.red);
            SetPixelSafe(pixels, SpriteSize, 20, 18, Color.red);

            // 하이라이트
            SetPixelSafe(pixels, SpriteSize, 10, 20, highlight);
            SetPixelSafe(pixels, SpriteSize, 11, 21, highlight);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// NPC 스프라이트 생성 (인간형 + 모자/표식)
        /// </summary>
        private static Texture2D CreateNPCSprite(Color baseColor)
        {
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color outline = baseColor * 0.5f;
            outline.a = 1f;
            Color hatColor = Color.Lerp(baseColor, Color.white, 0.4f);

            Color[] pixels = new Color[SpriteSize * SpriteSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // 머리
            DrawCircle(pixels, SpriteSize, 16, 24, 5, baseColor);

            // 모자/표식 (NPC 구별용)
            FillRect(pixels, SpriteSize, 10, 28, 22, 31, hatColor);

            // 몸통 (로브 형태)
            FillRect(pixels, SpriteSize, 10, 8, 22, 19, baseColor);
            DrawRect(pixels, SpriteSize, 9, 7, 23, 20, outline);

            // 다리
            FillRect(pixels, SpriteSize, 12, 2, 15, 8, baseColor);
            FillRect(pixels, SpriteSize, 17, 2, 20, 8, baseColor);

            // 눈
            SetPixelSafe(pixels, SpriteSize, 14, 25, Color.white);
            SetPixelSafe(pixels, SpriteSize, 18, 25, Color.white);

            // 느낌표 마크 (NPC 인디케이터)
            SetPixelSafe(pixels, SpriteSize, 16, 31, Color.yellow);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // --- 그리기 헬퍼 ---

        private static void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
                pixels[y * size + x] = color;
        }

        private static void FillRect(Color[] pixels, int size, int x1, int y1, int x2, int y2, Color color)
        {
            for (int y = y1; y <= y2; y++)
                for (int x = x1; x <= x2; x++)
                    SetPixelSafe(pixels, size, x, y, color);
        }

        private static void DrawRect(Color[] pixels, int size, int x1, int y1, int x2, int y2, Color color)
        {
            for (int x = x1; x <= x2; x++)
            {
                SetPixelSafe(pixels, size, x, y1, color);
                SetPixelSafe(pixels, size, x, y2, color);
            }
            for (int y = y1; y <= y2; y++)
            {
                SetPixelSafe(pixels, size, x1, y, color);
                SetPixelSafe(pixels, size, x2, y, color);
            }
        }

        private static void DrawCircle(Color[] pixels, int size, int cx, int cy, int radius, Color color, bool outlineOnly = false)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (outlineOnly)
                    {
                        if (dist >= radius - 0.5f && dist <= radius + 0.5f)
                            SetPixelSafe(pixels, size, x, y, color);
                    }
                    else
                    {
                        if (dist <= radius)
                            SetPixelSafe(pixels, size, x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipse(Color[] pixels, int size, int cx, int cy, int rx, int ry, Color color, bool outlineOnly = false)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float dx = (float)(x - cx) / rx;
                    float dy = (float)(y - cy) / ry;
                    float dist = dx * dx + dy * dy;
                    if (outlineOnly)
                    {
                        if (dist >= 0.8f && dist <= 1.2f)
                            SetPixelSafe(pixels, size, x, y, color);
                    }
                    else
                    {
                        if (dist <= 1f)
                            SetPixelSafe(pixels, size, x, y, color);
                    }
                }
            }
        }

        // --- 파일 I/O ---

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void SaveSprite(Texture2D texture, string path)
        {
            byte[] pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(path, pngData);

            // Unity에서 스프라이트로 인식하도록 임포트 설정
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
