using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 16개 직업 × 13개 스킬 = 208개 SkillData ScriptableObject 자동 생성 에디터 스크립트
    /// </summary>
    public class SkillDataGenerator : EditorWindow
    {
        private bool overwriteExisting = false;
        private Vector2 scrollPos;

        [MenuItem("Dungeon Crawler/Generate All Skill Data (208)")]
        public static void ShowWindow()
        {
            GetWindow<SkillDataGenerator>("Skill Data Generator");
        }

        [MenuItem("Dungeon Crawler/Auto Generate All Skills Now")]
        public static void AutoGenerateAll()
        {
            var generator = CreateInstance<SkillDataGenerator>();
            generator.overwriteExisting = true;
            generator.GenerateAllSkillData();
            DestroyImmediate(generator);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("스킬 데이터 자동 생성기", EditorStyles.boldLabel);
            GUILayout.Space(5);
            GUILayout.Label("16개 직업 × 13개 스킬 = 208개 SkillData 생성", EditorStyles.helpBox);
            GUILayout.Space(10);

            overwriteExisting = EditorGUILayout.Toggle("기존 데이터 덮어쓰기", overwriteExisting);
            GUILayout.Space(10);

            if (GUILayout.Button("전체 스킬 데이터 생성 (208개)", GUILayout.Height(40)))
            {
                GenerateAllSkillData();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("특정 직업만 생성", GUILayout.Height(30)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var job in GetAllJobDefinitions())
                {
                    string jobName = job.jobName;
                    menu.AddItem(new GUIContent(jobName), false, () => GenerateJobSkills(job));
                }
                menu.ShowAsContext();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 전체 208개 스킬 데이터 생성
        /// </summary>
        private void GenerateAllSkillData()
        {
            var jobs = GetAllJobDefinitions();
            int totalCreated = 0;

            foreach (var job in jobs)
            {
                totalCreated += GenerateJobSkills(job);
            }

            // JobData에 스킬 참조 연결
            LinkSkillsToJobData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SkillDataGenerator] {totalCreated}개 스킬 생성 완료. 경로: Assets/Resources/Skills/");
        }

        /// <summary>
        /// 특정 직업의 13개 스킬 생성
        /// </summary>
        private int GenerateJobSkills(JobDefinition job)
        {
            string folderPath = $"Assets/Resources/Skills/{job.jobName}";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = "Assets/Resources/Skills";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    AssetDatabase.CreateFolder("Assets/Resources", "Skills");
                }
                AssetDatabase.CreateFolder(parentFolder, job.jobName);
            }

            int created = 0;
            var skills = job.skills;

            for (int i = 0; i < skills.Length; i++)
            {
                var skillDef = skills[i];
                string assetPath = $"{folderPath}/{skillDef.fileName}.asset";

                if (!overwriteExisting && File.Exists(assetPath))
                {
                    continue;
                }

                var skillData = ScriptableObject.CreateInstance<SkillData>();
                ApplySkillDefinition(skillData, skillDef, job);

                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(skillData, assetPath);
                created++;
            }

            Debug.Log($"[SkillDataGenerator] {job.jobName}: {created}개 스킬 생성");
            return created;
        }

        /// <summary>
        /// 스킬 정의를 SkillData에 적용
        /// </summary>
        private void ApplySkillDefinition(SkillData skill, SkillDefinition def, JobDefinition job)
        {
            skill.skillName = def.skillName;
            skill.skillId = $"{job.jobName}_{def.fileName}";
            skill.description = def.description;
            skill.requiredLevel = def.requiredLevel;
            skill.goldCost = def.goldCost;
            skill.requiredRace = Race.Human; // 모든 종족 사용 가능 (직업으로 제한)
            skill.category = job.category;
            skill.skillTier = def.tier;

            skill.skillType = def.skillType;
            skill.damageType = def.damageType;
            skill.cooldown = def.cooldown;
            skill.manaCost = def.manaCost;
            skill.castTime = def.castTime;
            skill.range = def.range;

            skill.baseDamage = def.baseDamage;
            skill.damageScaling = def.damageScaling;
            skill.minDamagePercent = def.minDamagePercent;
            skill.maxDamagePercent = def.maxDamagePercent;

            skill.statBonus = def.statBonus;
            skill.healthBonus = def.healthBonus;
            skill.manaBonus = def.manaBonus;
            skill.moveSpeedBonus = def.moveSpeedBonus;
            skill.attackSpeedBonus = def.attackSpeedBonus;

            skill.behaviorType = def.behaviorType;

            if (def.statusEffects != null)
            {
                skill.statusEffects = def.statusEffects;
                skill.statusDuration = def.statusDuration;
                skill.statusChance = def.statusChance;
            }
        }

        /// <summary>
        /// JobData 에셋에 스킬 참조 연결
        /// </summary>
        private void LinkSkillsToJobData()
        {
            var jobs = GetAllJobDefinitions();

            foreach (var job in jobs)
            {
                string jobAssetPath = $"Assets/Resources/Jobs/{job.jobName}.asset";
                var jobData = AssetDatabase.LoadAssetAtPath<JobData>(jobAssetPath);
                if (jobData == null) continue;

                if (jobData.skillSet == null)
                    jobData.skillSet = new JobSkillSet();

                string skillFolder = $"Assets/Resources/Skills/{job.jobName}";

                // Level 1 Skills
                jobData.skillSet.level1Skills = LoadSkillChoice(skillFolder, "Level1", job.skills);
                // Level 3 Skills
                jobData.skillSet.level3Skills = LoadSkillChoice(skillFolder, "Level3", job.skills);
                // Level 5 Skills
                jobData.skillSet.level5Skills = LoadSkillChoice(skillFolder, "Level5", job.skills);
                // Level 7 Skills
                jobData.skillSet.level7Skills = LoadSkillChoice(skillFolder, "Level7", job.skills);
                // Ultimate
                var ultimate = AssetDatabase.LoadAssetAtPath<SkillData>($"{skillFolder}/Ultimate.asset");
                jobData.skillSet.ultimateSkill = ultimate;

                EditorUtility.SetDirty(jobData);
            }
        }

        /// <summary>
        /// 스킬 선택지 로드
        /// </summary>
        private SkillChoice LoadSkillChoice(string folder, string levelPrefix, SkillDefinition[] allSkills)
        {
            var choice = new SkillChoice();
            choice.choiceA = AssetDatabase.LoadAssetAtPath<SkillData>($"{folder}/{levelPrefix}_A.asset");
            choice.choiceB = AssetDatabase.LoadAssetAtPath<SkillData>($"{folder}/{levelPrefix}_B.asset");
            choice.choiceC = AssetDatabase.LoadAssetAtPath<SkillData>($"{folder}/{levelPrefix}_C.asset");

            // 골드 비용 설정
            foreach (var s in allSkills)
            {
                if (s.fileName == $"{levelPrefix}_A") choice.goldCostA = s.goldCost;
                if (s.fileName == $"{levelPrefix}_B") choice.goldCostB = s.goldCost;
                if (s.fileName == $"{levelPrefix}_C") choice.goldCostC = s.goldCost;
            }

            return choice;
        }

        // ====================================================================
        // 직업별 스킬 정의 데이터
        // ====================================================================

        private struct JobDefinition
        {
            public string jobName;
            public SkillCategory category;
            public SkillDefinition[] skills;
        }

        private struct SkillDefinition
        {
            public string fileName;
            public string skillName;
            public string description;
            public int requiredLevel;
            public long goldCost;
            public int tier;
            public SkillType skillType;
            public DamageType damageType;
            public float cooldown;
            public float manaCost;
            public float castTime;
            public float range;
            public float baseDamage;
            public float damageScaling;
            public float minDamagePercent;
            public float maxDamagePercent;
            public StatBlock statBonus;
            public float healthBonus;
            public float manaBonus;
            public float moveSpeedBonus;
            public float attackSpeedBonus;
            public SkillBehaviorType behaviorType;
            public StatusEffect[] statusEffects;
            public float statusDuration;
            public float statusChance;
        }

        /// <summary>
        /// 액티브 스킬 정의 생성 헬퍼
        /// </summary>
        private static SkillDefinition Active(string fileName, string name, string desc, int level, long gold, int tier,
            DamageType dmgType, float cooldown, float mana, float castTime, float range,
            float baseDmg, float scaling, SkillBehaviorType behavior = SkillBehaviorType.Instant,
            float minPct = 80f, float maxPct = 120f,
            StatusEffect[] effects = null, float statusDuration = 0f, float statusChance = 0f)
        {
            return new SkillDefinition
            {
                fileName = fileName, skillName = name, description = desc,
                requiredLevel = level, goldCost = gold, tier = tier,
                skillType = SkillType.Active, damageType = dmgType,
                cooldown = cooldown, manaCost = mana, castTime = castTime, range = range,
                baseDamage = baseDmg, damageScaling = scaling,
                minDamagePercent = minPct, maxDamagePercent = maxPct,
                behaviorType = behavior,
                statusEffects = effects, statusDuration = statusDuration, statusChance = statusChance
            };
        }

        /// <summary>
        /// 패시브 스킬 정의 생성 헬퍼
        /// </summary>
        private static SkillDefinition Passive(string fileName, string name, string desc, int level, long gold, int tier,
            StatBlock stats = default, float hp = 0f, float mp = 0f, float moveSpd = 0f, float atkSpd = 0f)
        {
            return new SkillDefinition
            {
                fileName = fileName, skillName = name, description = desc,
                requiredLevel = level, goldCost = gold, tier = tier,
                skillType = SkillType.Passive, damageType = DamageType.Physical,
                statBonus = stats, healthBonus = hp, manaBonus = mp,
                moveSpeedBonus = moveSpd, attackSpeedBonus = atkSpd,
                minDamagePercent = 80f, maxDamagePercent = 120f
            };
        }

        /// <summary>
        /// 모든 직업의 스킬 정의 반환
        /// </summary>
        private JobDefinition[] GetAllJobDefinitions()
        {
            return new JobDefinition[]
            {
                GetNavigatorSkills(),
                GetScoutSkills(),
                GetTrackerSkills(),
                GetTrapperSkills(),
                GetGuardianSkills(),
                GetTemplarSkills(),
                GetBerserkerSkills(),
                GetAssassinSkills(),
                GetDuelistSkills(),
                GetElementalBruiserSkills(),
                GetSniperSkills(),
                GetMageSkills(),
                GetWarlockSkills(),
                GetClericSkills(),
                GetDruidSkills(),
                GetAmplifierSkills(),
            };
        }

        // ====================================================================
        // 1. Navigator (항해사) - 정찰/항해 테마
        // ====================================================================
        private JobDefinition GetNavigatorSkills()
        {
            return new JobDefinition
            {
                jobName = "Navigator",
                category = SkillCategory.Warrior,
                skills = new SkillDefinition[]
                {
                    // Level 1
                    Active("Level1_A", "바람 가르기", "전방으로 날카로운 바람을 일으켜 적에게 피해를 준다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.5f, 3f, 15f, 0.8f),
                    Active("Level1_B", "나침반 강타", "나침반으로 방향을 잡아 정확한 일격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 4f, 10f, 0.8f, 2f, 20f, 1.0f),
                    Passive("Level1_C", "항해 감각", "오랜 항해 경험으로 민첩성과 운이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { agility = 3, luck = 2 }),

                    // Level 3
                    Active("Level3_A", "해류 베기", "해류의 힘을 담아 넓은 범위를 베어낸다.", 3, 150, 2,
                        DamageType.Physical, 5f, 15f, 0.6f, 4f, 30f, 1.0f),
                    Active("Level3_B", "풍향 사격", "바람의 방향을 읽어 원거리 공격의 정확도를 높인다.", 3, 150, 2,
                        DamageType.Physical, 6f, 12f, 0.4f, 8f, 25f, 0.9f, SkillBehaviorType.Projectile),
                    Passive("Level3_C", "노련한 선원", "전투 경험이 쌓여 체력과 방어력이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { vitality = 4, defense = 3 }, hp: 20f),

                    // Level 5
                    Active("Level5_A", "폭풍 돌진", "폭풍처럼 적진에 돌진하여 광범위 피해를 준다.", 5, 300, 3,
                        DamageType.Physical, 8f, 25f, 1.0f, 5f, 50f, 1.2f),
                    Active("Level5_B", "조류 제어", "주변의 조류를 조종하여 적의 이동을 방해한다.", 5, 300, 3,
                        DamageType.Physical, 10f, 20f, 0.8f, 6f, 35f, 0.8f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 30f, duration = 4f } },
                        statusDuration: 4f, statusChance: 80f),
                    Passive("Level5_C", "항로 마스터", "완벽한 항로 파악으로 이동속도와 공격속도가 증가한다.", 5, 300, 3,
                        stats: new StatBlock { agility = 5 }, moveSpd: 0.15f, atkSpd: 0.1f),

                    // Level 7
                    Active("Level7_A", "대양의 분노", "대양의 분노를 담아 강력한 일격을 가한다.", 7, 500, 4,
                        DamageType.Physical, 12f, 35f, 1.2f, 4f, 80f, 1.5f),
                    Active("Level7_B", "해적 난무", "해적의 전투술로 연속 공격을 가한다.", 7, 500, 4,
                        DamageType.Physical, 10f, 30f, 0.3f, 3f, 60f, 1.3f, minPct: 70f, maxPct: 130f),
                    Passive("Level7_C", "선장의 위엄", "선장으로서의 위엄이 모든 능력치를 높인다.", 7, 500, 4,
                        stats: new StatBlock { strength = 5, agility = 4, vitality = 3, luck = 3 }, hp: 30f),

                    // Ultimate
                    Active("Ultimate", "크라켄 소환", "전설의 크라켄을 소환하여 주변 모든 적에게 막대한 피해를 준다.", 10, 1000, 5,
                        DamageType.Physical, 30f, 60f, 2.0f, 8f, 150f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 2. Scout (정찰병) - 정찰/은신 테마
        // ====================================================================
        private JobDefinition GetScoutSkills()
        {
            return new JobDefinition
            {
                jobName = "Scout",
                category = SkillCategory.Stealth,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "은밀한 일격", "숨어서 접근하여 적의 약점을 공격한다.", 1, 50, 1,
                        DamageType.Physical, 4f, 10f, 0.3f, 2f, 18f, 1.1f),
                    Active("Level1_B", "정찰 사격", "적의 위치를 파악하며 원거리 공격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.5f, 10f, 12f, 0.7f, SkillBehaviorType.Projectile),
                    Passive("Level1_C", "경계 태세", "항상 경계하여 민첩과 회피율이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { agility = 4, luck = 1 }),

                    Active("Level3_A", "그림자 베기", "그림자에서 뛰쳐나와 급소를 공격한다.", 3, 150, 2,
                        DamageType.Physical, 5f, 14f, 0.4f, 2f, 35f, 1.2f),
                    Active("Level3_B", "연막탄", "연막을 터뜨려 적의 시야를 차단하고 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 8f, 18f, 0.6f, 5f, 20f, 0.6f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Weakness, value = 20f, duration = 5f } },
                        statusDuration: 5f, statusChance: 90f),
                    Passive("Level3_C", "은신 숙련", "은신 능력이 향상되어 이동속도가 증가한다.", 3, 150, 2,
                        stats: new StatBlock { agility = 3 }, moveSpd: 0.12f),

                    Active("Level5_A", "급소 관통", "적의 급소를 정확히 찔러 큰 피해를 준다.", 5, 300, 3,
                        DamageType.Physical, 7f, 22f, 0.5f, 2f, 55f, 1.4f, minPct: 90f, maxPct: 140f),
                    Active("Level5_B", "다중 사격", "여러 발의 화살을 동시에 발사한다.", 5, 300, 3,
                        DamageType.Physical, 6f, 20f, 0.8f, 8f, 40f, 1.0f, SkillBehaviorType.Projectile),
                    Passive("Level5_C", "생존 본능", "위험한 상황에서 체력과 방어력이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { vitality = 5, defense = 3 }, hp: 25f),

                    Active("Level7_A", "암살 기습", "완벽한 은신 후 치명적 기습 공격을 가한다.", 7, 500, 4,
                        DamageType.Physical, 14f, 35f, 0.3f, 2f, 90f, 1.6f),
                    Active("Level7_B", "독화살 난사", "독 바른 화살을 연속 발사한다.", 7, 500, 4,
                        DamageType.Poison, 10f, 28f, 0.6f, 9f, 60f, 1.1f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 8f, duration = 6f, tickInterval = 1f } },
                        statusDuration: 6f, statusChance: 75f),
                    Passive("Level7_C", "정찰 대장", "정찰 경험으로 모든 스탯이 고르게 증가한다.", 7, 500, 4,
                        stats: new StatBlock { strength = 3, agility = 5, vitality = 2, intelligence = 2, luck = 3 }),

                    Active("Ultimate", "그림자 폭풍", "그림자의 힘을 결집하여 주변 적들에게 연속 공격을 가한다.", 10, 1000, 5,
                        DamageType.Physical, 25f, 55f, 1.5f, 6f, 130f, 1.8f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Invisibility, value = 1f, duration = 3f } },
                        statusDuration: 3f, statusChance: 100f),
                }
            };
        }

        // ====================================================================
        // 3. Tracker (추적자) - 추적/사냥 테마
        // ====================================================================
        private JobDefinition GetTrackerSkills()
        {
            return new JobDefinition
            {
                jobName = "Tracker",
                category = SkillCategory.Hunt,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "추적 사격", "대상을 추적하여 정확한 사격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.5f, 10f, 14f, 0.9f, SkillBehaviorType.Projectile),
                    Active("Level1_B", "사냥꾼의 칼날", "사냥용 단검으로 빠르게 베어낸다.", 1, 50, 1,
                        DamageType.Physical, 2.5f, 7f, 0.3f, 2f, 16f, 1.0f),
                    Passive("Level1_C", "야생 감각", "야생에서의 감각이 발달하여 민첩과 체력이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { agility = 3, vitality = 2 }),

                    Active("Level3_A", "올가미 투척", "올가미를 던져 적을 속박하고 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 8f, 15f, 0.7f, 6f, 22f, 0.7f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Root, value = 1f, duration = 3f } },
                        statusDuration: 3f, statusChance: 85f),
                    Active("Level3_B", "야수의 발톱", "야수처럼 날카로운 공격으로 적을 찢어낸다.", 3, 150, 2,
                        DamageType.Physical, 4f, 12f, 0.4f, 2f, 32f, 1.1f),
                    Passive("Level3_C", "추적 본능", "추적 본능이 강화되어 이동속도와 치명타가 증가한다.", 3, 150, 2,
                        stats: new StatBlock { agility = 4, luck = 3 }, moveSpd: 0.1f),

                    Active("Level5_A", "연속 사격", "빠르게 연속으로 화살을 발사한다.", 5, 300, 3,
                        DamageType.Physical, 5f, 20f, 0.3f, 9f, 45f, 1.1f, SkillBehaviorType.Projectile),
                    Active("Level5_B", "맹수 소환", "훈련된 맹수를 소환하여 적을 공격한다.", 5, 300, 3,
                        DamageType.Physical, 12f, 25f, 1.0f, 5f, 40f, 1.0f, SkillBehaviorType.Summon),
                    Passive("Level5_C", "사냥꾼의 직감", "위험을 감지하는 직감으로 방어력과 회피가 증가한다.", 5, 300, 3,
                        stats: new StatBlock { agility = 5, defense = 4 }, hp: 20f),

                    Active("Level7_A", "관통 사격", "강력한 관통력으로 일직선의 모든 적에게 피해를 준다.", 7, 500, 4,
                        DamageType.Physical, 10f, 30f, 0.8f, 12f, 75f, 1.4f, SkillBehaviorType.Projectile),
                    Active("Level7_B", "사냥꾼의 표식", "적에게 표식을 남겨 받는 피해를 증가시킨다.", 7, 500, 4,
                        DamageType.Physical, 15f, 25f, 0.5f, 8f, 50f, 1.0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Weakness, value = 25f, duration = 8f } },
                        statusDuration: 8f, statusChance: 100f),
                    Passive("Level7_C", "대사냥꾼", "대사냥꾼의 경험으로 공격력과 민첩이 크게 증가한다.", 7, 500, 4,
                        stats: new StatBlock { strength = 5, agility = 6, luck = 4 }),

                    Active("Ultimate", "죽음의 사냥", "대상을 죽음의 사냥감으로 지정하여 극한의 피해를 입힌다.", 10, 1000, 5,
                        DamageType.Physical, 30f, 50f, 1.5f, 10f, 140f, 2.0f, SkillBehaviorType.Projectile),
                }
            };
        }

        // ====================================================================
        // 4. Trapper (함정 전문가) - 함정/설치 테마
        // ====================================================================
        private JobDefinition GetTrapperSkills()
        {
            return new JobDefinition
            {
                jobName = "Trapper",
                category = SkillCategory.Stealth,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "가시 함정", "바닥에 가시 함정을 설치하여 적에게 피해를 준다.", 1, 50, 1,
                        DamageType.Physical, 5f, 10f, 0.8f, 4f, 12f, 0.6f, SkillBehaviorType.Summon),
                    Active("Level1_B", "독침 투척", "독이 묻은 침을 던져 적에게 지속 피해를 준다.", 1, 50, 1,
                        DamageType.Poison, 4f, 8f, 0.4f, 6f, 10f, 0.5f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 5f, duration = 4f, tickInterval = 1f } },
                        statusDuration: 4f, statusChance: 80f),
                    Passive("Level1_C", "함정 제작술", "함정 제작 실력으로 지능과 민첩이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 3, agility = 2 }),

                    Active("Level3_A", "폭발 함정", "폭발하는 함정을 설치하여 범위 피해를 준다.", 3, 150, 2,
                        DamageType.Fire, 8f, 18f, 1.0f, 5f, 28f, 0.8f, SkillBehaviorType.Summon),
                    Active("Level3_B", "철조망", "철조망을 설치하여 적의 이동을 방해하고 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 10f, 15f, 1.2f, 4f, 18f, 0.5f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 40f, duration = 5f } },
                        statusDuration: 5f, statusChance: 90f),
                    Passive("Level3_C", "공학 지식", "공학 지식으로 함정의 효율이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { intelligence = 5, luck = 2 }),

                    Active("Level5_A", "연쇄 함정", "연쇄적으로 폭발하는 함정을 설치한다.", 5, 300, 3,
                        DamageType.Fire, 10f, 28f, 1.5f, 6f, 48f, 1.0f, SkillBehaviorType.Summon),
                    Active("Level5_B", "독가스 함정", "독가스를 뿜는 함정을 설치한다.", 5, 300, 3,
                        DamageType.Poison, 12f, 22f, 1.0f, 5f, 35f, 0.8f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 10f, duration = 6f, tickInterval = 1f } },
                        statusDuration: 6f, statusChance: 85f),
                    Passive("Level5_C", "생존 전문가", "야외 생존 능력으로 체력과 방어력이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { vitality = 5, defense = 4 }, hp: 30f),

                    Active("Level7_A", "지뢰밭", "넓은 범위에 지뢰를 설치하여 큰 피해를 준다.", 7, 500, 4,
                        DamageType.Fire, 15f, 40f, 2.0f, 8f, 70f, 1.2f, SkillBehaviorType.Summon),
                    Active("Level7_B", "마비 함정", "적을 마비시키는 강력한 함정을 설치한다.", 7, 500, 4,
                        DamageType.Lightning, 12f, 32f, 1.5f, 6f, 55f, 1.0f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 3f } },
                        statusDuration: 3f, statusChance: 70f),
                    Passive("Level7_C", "함정 마스터", "모든 함정의 효과가 강화된다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 6, agility = 4, luck = 5 }),

                    Active("Ultimate", "죽음의 미로", "주변에 죽음의 미로를 만들어 적에게 막대한 피해를 준다.", 10, 1000, 5,
                        DamageType.Physical, 30f, 65f, 2.5f, 10f, 130f, 1.5f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 5. Guardian (수호기사) - 방어/보호 테마
        // ====================================================================
        private JobDefinition GetGuardianSkills()
        {
            return new JobDefinition
            {
                jobName = "Guardian",
                category = SkillCategory.Warrior,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "방패 강타", "방패로 적을 강하게 타격한다.", 1, 50, 1,
                        DamageType.Physical, 4f, 10f, 0.6f, 2f, 18f, 0.9f),
                    Active("Level1_B", "도발", "적을 도발하여 자신에게 공격을 유도한다.", 1, 50, 1,
                        DamageType.Physical, 8f, 5f, 0.3f, 5f, 5f, 0.3f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Weakness, value = 15f, duration = 5f } },
                        statusDuration: 5f, statusChance: 100f),
                    Passive("Level1_C", "철벽 수비", "방어력과 체력이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { defense = 5, vitality = 3 }, hp: 15f),

                    Active("Level3_A", "방패 밀치기", "방패로 적을 밀어내며 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 5f, 12f, 0.5f, 3f, 28f, 0.8f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 1.5f } },
                        statusDuration: 1.5f, statusChance: 60f),
                    Active("Level3_B", "수호의 빛", "주변 아군의 방어력을 일시적으로 높인다.", 3, 150, 2,
                        DamageType.Holy, 12f, 20f, 1.0f, 6f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Shield, value = 30f, duration = 8f } },
                        statusDuration: 8f, statusChance: 100f),
                    Passive("Level3_C", "강인한 육체", "육체가 강인해져 체력과 마법 방어력이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { vitality = 5, magicDefense = 4 }, hp: 30f),

                    Active("Level5_A", "대지 분쇄", "땅을 내리쳐 주변 적에게 피해를 주고 감속시킨다.", 5, 300, 3,
                        DamageType.Physical, 8f, 25f, 1.0f, 4f, 45f, 1.0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 35f, duration = 4f } },
                        statusDuration: 4f, statusChance: 80f),
                    Active("Level5_B", "수호 결계", "보호 결계를 만들어 모든 피해를 일시적으로 감소시킨다.", 5, 300, 3,
                        DamageType.Holy, 15f, 30f, 1.5f, 7f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Shield, value = 50f, duration = 6f } },
                        statusDuration: 6f, statusChance: 100f),
                    Passive("Level5_C", "수호자의 맹세", "수호의 맹세로 방어력과 힘이 크게 증가한다.", 5, 300, 3,
                        stats: new StatBlock { defense = 8, strength = 4, vitality = 3 }, hp: 40f),

                    Active("Level7_A", "성스러운 방패", "성스러운 힘을 방패에 담아 강력한 공격을 가한다.", 7, 500, 4,
                        DamageType.Holy, 10f, 35f, 1.0f, 3f, 75f, 1.3f),
                    Active("Level7_B", "철벽 방어", "완벽한 방어 자세로 잠시 동안 무적이 된다.", 7, 500, 4,
                        DamageType.Physical, 20f, 40f, 0.5f, 1f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Shield, value = 100f, duration = 4f } },
                        statusDuration: 4f, statusChance: 100f),
                    Passive("Level7_C", "대수호자", "전설적인 수호력으로 모든 방어 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { defense = 10, magicDefense = 6, vitality = 5 }, hp: 60f),

                    Active("Ultimate", "신성한 보루", "거대한 신성한 보루를 소환하여 주변 적에게 성스러운 피해를 주고 아군을 보호한다.", 10, 1000, 5,
                        DamageType.Holy, 30f, 60f, 2.0f, 8f, 120f, 1.5f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 6. Templar (성기사) - 신성/방어 테마
        // ====================================================================
        private JobDefinition GetTemplarSkills()
        {
            return new JobDefinition
            {
                jobName = "Templar",
                category = SkillCategory.Paladin,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "신성한 일격", "신성한 힘을 담아 적을 공격한다.", 1, 50, 1,
                        DamageType.Holy, 3f, 10f, 0.6f, 2f, 17f, 0.9f),
                    Active("Level1_B", "정화의 빛", "빛의 힘으로 적에게 피해를 주고 자신을 치유한다.", 1, 50, 1,
                        DamageType.Holy, 5f, 12f, 0.8f, 3f, 12f, 0.7f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 5f, duration = 3f, tickInterval = 1f } },
                        statusDuration: 3f, statusChance: 100f),
                    Passive("Level1_C", "신앙의 힘", "신앙심으로 힘과 방어력이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { strength = 3, defense = 3 }),

                    Active("Level3_A", "심판의 검", "정의의 심판을 내려 강력한 공격을 가한다.", 3, 150, 2,
                        DamageType.Holy, 5f, 15f, 0.7f, 3f, 30f, 1.0f),
                    Active("Level3_B", "축복의 오라", "아군에게 축복을 내려 능력치를 높인다.", 3, 150, 2,
                        DamageType.Holy, 15f, 20f, 1.0f, 8f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Blessing, value = 15f, duration = 10f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level3_C", "성스러운 갑옷", "신성한 힘이 갑옷에 깃들어 방어력이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { defense = 6, magicDefense = 4 }, hp: 25f),

                    Active("Level5_A", "천벌", "하늘에서 신성한 번개를 내려 적에게 큰 피해를 준다.", 5, 300, 3,
                        DamageType.Holy, 8f, 28f, 1.0f, 7f, 50f, 1.2f, SkillBehaviorType.Summon),
                    Active("Level5_B", "치유의 기도", "기도를 올려 자신과 주변 아군의 체력을 회복한다.", 5, 300, 3,
                        DamageType.Holy, 10f, 25f, 1.5f, 6f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 15f, duration = 8f, tickInterval = 1f } },
                        statusDuration: 8f, statusChance: 100f),
                    Passive("Level5_C", "성전사의 의지", "성전사의 의지로 모든 스탯이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { strength = 4, defense = 5, vitality = 4, intelligence = 3 }, hp: 35f),

                    Active("Level7_A", "신의 분노", "신의 분노를 불러내 주변 적에게 막대한 신성 피해를 준다.", 7, 500, 4,
                        DamageType.Holy, 12f, 40f, 1.5f, 6f, 85f, 1.5f, SkillBehaviorType.Summon),
                    Active("Level7_B", "불멸의 축복", "신의 축복으로 잠시 동안 불멸 상태가 된다.", 7, 500, 4,
                        DamageType.Holy, 25f, 50f, 1.0f, 1f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Shield, value = 100f, duration = 5f } },
                        statusDuration: 5f, statusChance: 100f),
                    Passive("Level7_C", "성기사의 위엄", "성기사로서의 위엄이 힘과 방어를 극대화한다.", 7, 500, 4,
                        stats: new StatBlock { strength = 7, defense = 8, magicDefense = 5, vitality = 4 }, hp: 50f),

                    Active("Ultimate", "신의 심판", "절대적인 신의 심판을 내려 모든 적에게 치명적인 신성 피해를 입힌다.", 10, 1000, 5,
                        DamageType.Holy, 35f, 70f, 2.5f, 10f, 160f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 7. Berserker (광전사) - 분노/물리 테마
        // ====================================================================
        private JobDefinition GetBerserkerSkills()
        {
            return new JobDefinition
            {
                jobName = "Berserker",
                category = SkillCategory.Berserker,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "광폭 타격", "분노로 강화된 강력한 타격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.4f, 2f, 22f, 1.1f, minPct: 70f, maxPct: 140f),
                    Active("Level1_B", "돌진", "적에게 돌진하여 피해를 주고 밀어낸다.", 1, 50, 1,
                        DamageType.Physical, 5f, 10f, 0.3f, 5f, 15f, 0.8f),
                    Passive("Level1_C", "야성 해방", "야성의 힘으로 공격력이 크게 증가한다.", 1, 50, 1,
                        stats: new StatBlock { strength = 5 }),

                    Active("Level3_A", "분노 폭발", "쌓인 분노를 폭발시켜 주변 적에게 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 6f, 15f, 0.5f, 4f, 35f, 1.2f),
                    Active("Level3_B", "피의 일격", "피를 쏟으며 강력한 한 방을 날린다.", 3, 150, 2,
                        DamageType.Physical, 5f, 18f, 0.6f, 2f, 40f, 1.3f, minPct: 60f, maxPct: 150f),
                    Passive("Level3_C", "광전사의 인내", "광전사의 인내력으로 체력이 크게 증가한다.", 3, 150, 2,
                        stats: new StatBlock { vitality = 6, strength = 2 }, hp: 35f),

                    Active("Level5_A", "지옥 난무", "지옥의 힘으로 연속 공격을 가한다.", 5, 300, 3,
                        DamageType.Physical, 7f, 25f, 0.3f, 3f, 55f, 1.4f, minPct: 60f, maxPct: 160f),
                    Active("Level5_B", "전쟁 함성", "전쟁의 함성으로 자신을 강화하고 적을 약화시킨다.", 5, 300, 3,
                        DamageType.Physical, 12f, 20f, 0.5f, 6f, 25f, 0.5f,
                        effects: new StatusEffect[] {
                            new StatusEffect { type = StatusType.Berserk, value = 30f, duration = 8f },
                            new StatusEffect { type = StatusType.Weakness, value = 20f, duration = 5f } },
                        statusDuration: 8f, statusChance: 100f),
                    Passive("Level5_C", "피의 갈증", "전투에서 체력 흡수 능력이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { strength = 6, vitality = 4 }, hp: 25f, atkSpd: 0.1f),

                    Active("Level7_A", "멸절의 일격", "모든 분노를 담은 파괴적인 일격을 가한다.", 7, 500, 4,
                        DamageType.Physical, 15f, 40f, 1.0f, 3f, 100f, 1.8f, minPct: 50f, maxPct: 180f),
                    Active("Level7_B", "광폭화", "완전한 광폭 상태에 빠져 공격력이 극대화된다.", 7, 500, 4,
                        DamageType.Physical, 20f, 35f, 0.5f, 1f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Berserk, value = 50f, duration = 10f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level7_C", "불굴의 의지", "어떤 상황에서도 포기하지 않는 의지로 모든 전투 능력이 증가한다.", 7, 500, 4,
                        stats: new StatBlock { strength = 8, vitality = 6, defense = 3 }, hp: 50f, atkSpd: 0.15f),

                    Active("Ultimate", "라그나로크", "모든 것을 파괴하는 궁극의 분노를 해방한다.", 10, 1000, 5,
                        DamageType.Physical, 30f, 60f, 1.5f, 6f, 180f, 2.5f, minPct: 50f, maxPct: 200f),
                }
            };
        }

        // ====================================================================
        // 8. Assassin (암살자) - 암살/독 테마
        // ====================================================================
        private JobDefinition GetAssassinSkills()
        {
            return new JobDefinition
            {
                jobName = "Assassin",
                category = SkillCategory.Assassin,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "독 바른 칼날", "독이 묻은 단검으로 적을 공격한다.", 1, 50, 1,
                        DamageType.Poison, 3f, 8f, 0.3f, 2f, 14f, 0.9f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 4f, duration = 4f, tickInterval = 1f } },
                        statusDuration: 4f, statusChance: 70f),
                    Active("Level1_B", "기습", "등 뒤에서 기습 공격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 4f, 10f, 0.2f, 2f, 20f, 1.2f),
                    Passive("Level1_C", "암살 기술", "암살 기술이 향상되어 치명타와 민첩이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { agility = 4, luck = 3 }),

                    Active("Level3_A", "연속 자상", "단검으로 빠르게 여러 번 찌른다.", 3, 150, 2,
                        DamageType.Physical, 4f, 12f, 0.2f, 2f, 32f, 1.1f),
                    Active("Level3_B", "맹독 주입", "치명적인 독을 적에게 주입한다.", 3, 150, 2,
                        DamageType.Poison, 6f, 15f, 0.5f, 2f, 18f, 0.7f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 8f, duration = 6f, tickInterval = 1f } },
                        statusDuration: 6f, statusChance: 90f),
                    Passive("Level3_C", "그림자 걸음", "그림자처럼 움직여 이동속도와 회피가 증가한다.", 3, 150, 2,
                        stats: new StatBlock { agility = 5 }, moveSpd: 0.15f),

                    Active("Level5_A", "급소 찌르기", "적의 급소를 정확히 찔러 치명적인 피해를 준다.", 5, 300, 3,
                        DamageType.Physical, 6f, 22f, 0.3f, 2f, 60f, 1.5f, minPct: 90f, maxPct: 150f),
                    Active("Level5_B", "독안개", "독안개를 뿌려 범위 내 적에게 지속 피해를 준다.", 5, 300, 3,
                        DamageType.Poison, 10f, 25f, 0.8f, 5f, 30f, 0.8f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 12f, duration = 8f, tickInterval = 1f } },
                        statusDuration: 8f, statusChance: 85f),
                    Passive("Level5_C", "치명 본능", "치명적인 공격 본능으로 공격력이 크게 증가한다.", 5, 300, 3,
                        stats: new StatBlock { strength = 4, agility = 5, luck = 5 }, atkSpd: 0.1f),

                    Active("Level7_A", "사신의 일격", "사신의 힘을 빌려 한 방에 적을 쓰러뜨린다.", 7, 500, 4,
                        DamageType.Physical, 12f, 35f, 0.3f, 2f, 95f, 1.8f, minPct: 85f, maxPct: 160f),
                    Active("Level7_B", "독룡의 이빨", "전설의 독룡의 힘으로 적에게 극독을 주입한다.", 7, 500, 4,
                        DamageType.Poison, 10f, 30f, 0.5f, 3f, 55f, 1.2f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 20f, duration = 10f, tickInterval = 1f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level7_C", "암살 마스터", "최고의 암살자로서 모든 전투 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { strength = 5, agility = 8, luck = 6 }, moveSpd: 0.1f, atkSpd: 0.15f),

                    Active("Ultimate", "사신의 그림자", "사신의 그림자가 되어 주변 모든 적에게 치명적인 연속 공격을 가한다.", 10, 1000, 5,
                        DamageType.Physical, 25f, 55f, 1.0f, 5f, 160f, 2.2f),
                }
            };
        }

        // ====================================================================
        // 9. Duelist (결투가) - 결투/연속공격 테마
        // ====================================================================
        private JobDefinition GetDuelistSkills()
        {
            return new JobDefinition
            {
                jobName = "Duelist",
                category = SkillCategory.Combat,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "정밀 찌르기", "정확하게 약점을 찌르는 공격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.3f, 2f, 16f, 1.0f, minPct: 90f, maxPct: 110f),
                    Active("Level1_B", "반격 자세", "적의 공격을 읽고 반격한다.", 1, 50, 1,
                        DamageType.Physical, 5f, 10f, 0.2f, 2f, 18f, 1.1f),
                    Passive("Level1_C", "결투 자세", "결투 자세로 공격 정밀도가 증가한다.", 1, 50, 1,
                        stats: new StatBlock { strength = 2, agility = 3, stability = 3 }),

                    Active("Level3_A", "연속 베기", "빠른 속도로 3회 연속 공격을 가한다.", 3, 150, 2,
                        DamageType.Physical, 4f, 14f, 0.3f, 2f, 36f, 1.2f),
                    Active("Level3_B", "페인트 어택", "가짜 공격 후 진짜 공격을 가한다.", 3, 150, 2,
                        DamageType.Physical, 5f, 12f, 0.4f, 2f, 30f, 1.1f, minPct: 95f, maxPct: 120f),
                    Passive("Level3_C", "검술 연마", "검술 연마로 공격력과 안정성이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { strength = 4, stability = 4, agility = 2 }),

                    Active("Level5_A", "칼날 회전", "몸을 회전하며 주변 적에게 연속 피해를 준다.", 5, 300, 3,
                        DamageType.Physical, 7f, 22f, 0.5f, 3f, 50f, 1.3f),
                    Active("Level5_B", "일점 돌파", "한 점에 모든 힘을 집중하여 관통 공격을 가한다.", 5, 300, 3,
                        DamageType.Physical, 8f, 25f, 0.8f, 3f, 55f, 1.5f, minPct: 95f, maxPct: 130f),
                    Passive("Level5_C", "검사의 길", "검의 길을 걷는 자의 모든 능력이 향상된다.", 5, 300, 3,
                        stats: new StatBlock { strength = 5, agility = 5, stability = 3 }, atkSpd: 0.12f),

                    Active("Level7_A", "무한 연무", "끝없는 연속 공격으로 적을 몰아붙인다.", 7, 500, 4,
                        DamageType.Physical, 8f, 32f, 0.2f, 3f, 80f, 1.5f),
                    Active("Level7_B", "필살 일섬", "완벽한 타이밍의 한 번의 공격으로 적을 베어낸다.", 7, 500, 4,
                        DamageType.Physical, 14f, 35f, 0.8f, 3f, 95f, 1.8f, minPct: 95f, maxPct: 140f),
                    Passive("Level7_C", "검성의 경지", "검성의 경지에 올라 전투 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { strength = 7, agility = 6, stability = 5, luck = 3 }, atkSpd: 0.2f),

                    Active("Ultimate", "만검귀류", "수백 번의 검격을 순식간에 가하는 궁극의 검술.", 10, 1000, 5,
                        DamageType.Physical, 25f, 55f, 1.0f, 4f, 170f, 2.3f, minPct: 90f, maxPct: 150f),
                }
            };
        }

        // ====================================================================
        // 10. ElementalBruiser (원소 투사) - 원소/물리 혼합 테마
        // ====================================================================
        private JobDefinition GetElementalBruiserSkills()
        {
            return new JobDefinition
            {
                jobName = "ElementalBruiser",
                category = SkillCategory.Combat,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "화염 주먹", "주먹에 화염을 두르고 공격한다.", 1, 50, 1,
                        DamageType.Fire, 3f, 10f, 0.4f, 2f, 16f, 0.9f),
                    Active("Level1_B", "냉기 일격", "냉기를 담은 공격으로 적을 감속시킨다.", 1, 50, 1,
                        DamageType.Ice, 4f, 10f, 0.5f, 2f, 14f, 0.8f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 20f, duration = 3f } },
                        statusDuration: 3f, statusChance: 70f),
                    Passive("Level1_C", "원소 친화", "원소의 힘과 친화되어 힘과 지능이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { strength = 3, intelligence = 3 }),

                    Active("Level3_A", "번개 강타", "번개를 주먹에 담아 강력한 타격을 가한다.", 3, 150, 2,
                        DamageType.Lightning, 4f, 14f, 0.5f, 2f, 30f, 1.0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 1f } },
                        statusDuration: 1f, statusChance: 40f),
                    Active("Level3_B", "화염 회전", "화염을 두르고 회전하여 주변 적에게 피해를 준다.", 3, 150, 2,
                        DamageType.Fire, 6f, 16f, 0.6f, 4f, 28f, 0.9f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Burn, value = 6f, duration = 4f, tickInterval = 1f } },
                        statusDuration: 4f, statusChance: 60f),
                    Passive("Level3_C", "원소 각성", "원소의 힘이 각성하여 공격에 원소 피해가 추가된다.", 3, 150, 2,
                        stats: new StatBlock { strength = 3, intelligence = 4, vitality = 2 }),

                    Active("Level5_A", "빙화 폭발", "얼음과 화염의 충돌로 폭발적 피해를 준다.", 5, 300, 3,
                        DamageType.Fire, 7f, 25f, 0.8f, 5f, 52f, 1.2f),
                    Active("Level5_B", "뇌신 강타", "번개의 신이 내린 듯한 강력한 일격을 가한다.", 5, 300, 3,
                        DamageType.Lightning, 8f, 28f, 0.7f, 3f, 48f, 1.3f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 2f } },
                        statusDuration: 2f, statusChance: 55f),
                    Passive("Level5_C", "원소 갑옷", "원소의 힘으로 방어력과 저항력이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { defense = 5, magicDefense = 5, strength = 3, intelligence = 3 }),

                    Active("Level7_A", "삼원소 폭풍", "화염, 냉기, 번개를 동시에 해방한다.", 7, 500, 4,
                        DamageType.Fire, 10f, 38f, 1.0f, 6f, 85f, 1.5f),
                    Active("Level7_B", "원소 합체", "모든 원소를 몸에 합체시켜 강화 상태가 된다.", 7, 500, 4,
                        DamageType.Physical, 18f, 35f, 0.5f, 1f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Enhancement, value = 40f, duration = 10f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level7_C", "원소 마스터", "모든 원소를 마스터하여 전투 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { strength = 7, intelligence = 7, vitality = 4 }, atkSpd: 0.1f),

                    Active("Ultimate", "원소 대격변", "모든 원소의 힘을 해방하여 주변에 대재앙을 일으킨다.", 10, 1000, 5,
                        DamageType.Fire, 30f, 65f, 2.0f, 8f, 155f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 11. Sniper (저격수) - 저격/원거리 테마
        // ====================================================================
        private JobDefinition GetSniperSkills()
        {
            return new JobDefinition
            {
                jobName = "Sniper",
                category = SkillCategory.Archery,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "정밀 사격", "정확한 조준으로 적을 사격한다.", 1, 50, 1,
                        DamageType.Physical, 4f, 8f, 0.8f, 12f, 18f, 1.0f, SkillBehaviorType.Projectile, minPct: 90f, maxPct: 115f),
                    Active("Level1_B", "속사", "빠르게 연속 사격을 가한다.", 1, 50, 1,
                        DamageType.Physical, 3f, 6f, 0.2f, 10f, 12f, 0.7f, SkillBehaviorType.Projectile),
                    Passive("Level1_C", "명사수의 눈", "뛰어난 시력으로 정확도와 사거리가 증가한다.", 1, 50, 1,
                        stats: new StatBlock { agility = 3, luck = 3 }),

                    Active("Level3_A", "관통탄", "강력한 관통탄을 발사하여 일직선의 적에게 피해를 준다.", 3, 150, 2,
                        DamageType.Physical, 6f, 15f, 1.0f, 15f, 30f, 1.0f, SkillBehaviorType.Projectile),
                    Active("Level3_B", "화염탄", "불이 붙은 화살로 적에게 화상을 입힌다.", 3, 150, 2,
                        DamageType.Fire, 5f, 12f, 0.8f, 12f, 22f, 0.8f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Burn, value = 6f, duration = 4f, tickInterval = 1f } },
                        statusDuration: 4f, statusChance: 75f),
                    Passive("Level3_C", "차분한 조준", "차분한 마음으로 치명타 확률이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { agility = 4, luck = 4, stability = 3 }),

                    Active("Level5_A", "저격", "완벽한 조준 후 강력한 한 발을 발사한다.", 5, 300, 3,
                        DamageType.Physical, 10f, 25f, 2.0f, 20f, 65f, 1.5f, SkillBehaviorType.Projectile, minPct: 95f, maxPct: 130f),
                    Active("Level5_B", "화살 비", "하늘에서 화살 비를 내려 범위 피해를 준다.", 5, 300, 3,
                        DamageType.Physical, 8f, 22f, 1.0f, 10f, 42f, 1.0f, SkillBehaviorType.Summon),
                    Passive("Level5_C", "사격 달인", "사격 기술이 숙달되어 공격속도와 민첩이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { agility = 6, strength = 3, luck = 3 }, atkSpd: 0.15f),

                    Active("Level7_A", "헤드샷", "적의 머리를 정확히 조준하여 치명적 일격을 가한다.", 7, 500, 4,
                        DamageType.Physical, 14f, 35f, 2.5f, 25f, 100f, 2.0f, SkillBehaviorType.Projectile, minPct: 95f, maxPct: 140f),
                    Active("Level7_B", "폭발 화살", "폭발하는 화살로 범위 피해를 준다.", 7, 500, 4,
                        DamageType.Fire, 10f, 30f, 1.2f, 15f, 70f, 1.3f, SkillBehaviorType.Projectile),
                    Passive("Level7_C", "전설의 저격수", "전설의 저격수로서 원거리 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { agility = 8, luck = 6, strength = 4, stability = 5 }),

                    Active("Ultimate", "궁극의 일격", "모든 집중력을 담아 단 한 발로 적을 관통한다.", 10, 1000, 5,
                        DamageType.Physical, 35f, 60f, 3.0f, 30f, 200f, 2.5f, SkillBehaviorType.Projectile, minPct: 95f, maxPct: 150f),
                }
            };
        }

        // ====================================================================
        // 12. Mage (마법사) - 원소 마법 테마
        // ====================================================================
        private JobDefinition GetMageSkills()
        {
            return new JobDefinition
            {
                jobName = "Mage",
                category = SkillCategory.ElementalMage,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "파이어볼", "화염 구슬을 발사하여 적에게 화상을 입힌다.", 1, 50, 1,
                        DamageType.Fire, 3f, 12f, 0.8f, 8f, 16f, 1.0f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Burn, value = 3f, duration = 3f, tickInterval = 1f } },
                        statusDuration: 3f, statusChance: 50f),
                    Active("Level1_B", "아이스 볼트", "냉기 탄을 발사하여 적을 감속시킨다.", 1, 50, 1,
                        DamageType.Ice, 3f, 10f, 0.6f, 8f, 14f, 0.9f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 25f, duration = 3f } },
                        statusDuration: 3f, statusChance: 65f),
                    Passive("Level1_C", "마나 순환", "마나 순환 능력이 향상된다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 4 }, mp: 20f),

                    Active("Level3_A", "라이트닝", "번개를 소환하여 적에게 피해를 준다.", 3, 150, 2,
                        DamageType.Lightning, 4f, 16f, 0.7f, 9f, 28f, 1.1f, SkillBehaviorType.Projectile,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 1f } },
                        statusDuration: 1f, statusChance: 30f),
                    Active("Level3_B", "프로스트 노바", "냉기 폭발로 주변 적을 동결시킨다.", 3, 150, 2,
                        DamageType.Ice, 6f, 18f, 1.0f, 5f, 22f, 0.9f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Freeze, value = 1f, duration = 2f } },
                        statusDuration: 2f, statusChance: 50f),
                    Passive("Level3_C", "원소 집중", "원소 마법의 위력이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { intelligence = 6 }, mp: 30f),

                    Active("Level5_A", "메테오", "하늘에서 불타는 운석을 떨어뜨린다.", 5, 300, 3,
                        DamageType.Fire, 10f, 30f, 1.5f, 10f, 55f, 1.3f, SkillBehaviorType.Summon),
                    Active("Level5_B", "블리자드", "얼음 폭풍을 일으켜 범위 피해와 감속을 준다.", 5, 300, 3,
                        DamageType.Ice, 8f, 25f, 1.2f, 8f, 42f, 1.1f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Slow, value = 40f, duration = 5f } },
                        statusDuration: 5f, statusChance: 80f),
                    Passive("Level5_C", "마법 숙련", "모든 마법의 효율이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { intelligence = 7, luck = 3 }, mp: 40f),

                    Active("Level7_A", "썬더스톰", "번개 폭풍을 소환하여 범위 내 적에게 큰 피해를 준다.", 7, 500, 4,
                        DamageType.Lightning, 12f, 40f, 1.5f, 10f, 80f, 1.5f, SkillBehaviorType.Summon),
                    Active("Level7_B", "절대영도", "절대영도의 냉기로 적을 완전히 얼려버린다.", 7, 500, 4,
                        DamageType.Ice, 14f, 38f, 1.8f, 8f, 65f, 1.3f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Freeze, value = 1f, duration = 4f } },
                        statusDuration: 4f, statusChance: 70f),
                    Passive("Level7_C", "대마법사", "마법의 경지에 올라 모든 마법 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 10, magicDefense = 5, luck = 4 }, mp: 60f),

                    Active("Ultimate", "아마겟돈", "천지를 뒤흔드는 궁극의 파괴 마법을 시전한다.", 10, 1000, 5,
                        DamageType.Fire, 30f, 70f, 3.0f, 12f, 170f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 13. Warlock (흑마법사) - 흑마법/저주 테마
        // ====================================================================
        private JobDefinition GetWarlockSkills()
        {
            return new JobDefinition
            {
                jobName = "Warlock",
                category = SkillCategory.PsychicMage,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "암흑 탄환", "암흑 에너지를 발사한다.", 1, 50, 1,
                        DamageType.Dark, 3f, 10f, 0.6f, 8f, 15f, 0.9f, SkillBehaviorType.Projectile),
                    Active("Level1_B", "저주", "적에게 저주를 걸어 약화시킨다.", 1, 50, 1,
                        DamageType.Dark, 5f, 12f, 0.8f, 7f, 10f, 0.5f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Weakness, value = 20f, duration = 6f } },
                        statusDuration: 6f, statusChance: 80f),
                    Passive("Level1_C", "암흑 친화", "암흑의 힘과 친화되어 마법력이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 4, luck = 1 }),

                    Active("Level3_A", "생명 흡수", "적의 생명력을 흡수한다.", 3, 150, 2,
                        DamageType.Dark, 5f, 16f, 0.8f, 6f, 25f, 1.0f, SkillBehaviorType.Projectile),
                    Active("Level3_B", "공포", "적에게 공포를 심어 행동을 방해한다.", 3, 150, 2,
                        DamageType.Dark, 8f, 18f, 1.0f, 6f, 15f, 0.6f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Stun, value = 1f, duration = 2.5f } },
                        statusDuration: 2.5f, statusChance: 65f),
                    Passive("Level3_C", "어둠의 지식", "금지된 지식으로 마법력이 크게 증가한다.", 3, 150, 2,
                        stats: new StatBlock { intelligence = 6, magicDefense = 2 }, mp: 25f),

                    Active("Level5_A", "저주 폭발", "축적된 저주를 폭발시켜 큰 피해를 준다.", 5, 300, 3,
                        DamageType.Dark, 7f, 28f, 1.0f, 7f, 50f, 1.3f, SkillBehaviorType.Projectile),
                    Active("Level5_B", "영혼 속박", "적의 영혼을 속박하여 행동을 봉인한다.", 5, 300, 3,
                        DamageType.Dark, 10f, 25f, 1.2f, 6f, 30f, 0.8f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Root, value = 1f, duration = 4f } },
                        statusDuration: 4f, statusChance: 75f),
                    Passive("Level5_C", "암흑 계약", "어둠과의 계약으로 강력한 마법력을 얻는다.", 5, 300, 3,
                        stats: new StatBlock { intelligence = 8, luck = 3 }, mp: 40f),

                    Active("Level7_A", "심연의 폭풍", "심연의 에너지로 파괴적인 폭풍을 일으킨다.", 7, 500, 4,
                        DamageType.Dark, 12f, 42f, 1.5f, 9f, 85f, 1.6f, SkillBehaviorType.Summon),
                    Active("Level7_B", "죽음의 손길", "죽음의 힘으로 적의 생명력을 빼앗는다.", 7, 500, 4,
                        DamageType.Dark, 10f, 35f, 1.0f, 5f, 65f, 1.3f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Poison, value = 15f, duration = 8f, tickInterval = 1f } },
                        statusDuration: 8f, statusChance: 90f),
                    Passive("Level7_C", "대흑마법사", "흑마법의 정점에 올라 모든 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 10, magicDefense = 6, luck = 5 }, mp: 50f),

                    Active("Ultimate", "종말의 의식", "금지된 의식을 시전하여 주변의 모든 적에게 암흑의 심판을 내린다.", 10, 1000, 5,
                        DamageType.Dark, 35f, 75f, 3.0f, 10f, 165f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 14. Cleric (성직자) - 치유/신성 테마
        // ====================================================================
        private JobDefinition GetClericSkills()
        {
            return new JobDefinition
            {
                jobName = "Cleric",
                category = SkillCategory.Paladin,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "신성 타격", "신성한 힘으로 적을 공격한다.", 1, 50, 1,
                        DamageType.Holy, 4f, 10f, 0.6f, 5f, 14f, 0.8f, SkillBehaviorType.Projectile),
                    Active("Level1_B", "치유의 빛", "빛의 힘으로 체력을 회복한다.", 1, 50, 1,
                        DamageType.Holy, 3f, 12f, 1.0f, 6f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 8f, duration = 4f, tickInterval = 1f } },
                        statusDuration: 4f, statusChance: 100f),
                    Passive("Level1_C", "신앙심", "깊은 신앙으로 지능과 체력이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 3, vitality = 3 }, mp: 15f),

                    Active("Level3_A", "성스러운 심판", "성스러운 빛으로 적에게 피해를 준다.", 3, 150, 2,
                        DamageType.Holy, 5f, 16f, 0.8f, 7f, 25f, 1.0f, SkillBehaviorType.Projectile),
                    Active("Level3_B", "대치유", "강력한 치유 마법으로 대량의 체력을 회복한다.", 3, 150, 2,
                        DamageType.Holy, 6f, 20f, 1.2f, 8f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 15f, duration = 6f, tickInterval = 1f } },
                        statusDuration: 6f, statusChance: 100f),
                    Passive("Level3_C", "신의 은총", "신의 은총으로 마법 방어력과 지능이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { intelligence = 5, magicDefense = 5 }, mp: 25f, hp: 20f),

                    Active("Level5_A", "신성 폭발", "축적된 신성력을 폭발시켜 적에게 큰 피해를 준다.", 5, 300, 3,
                        DamageType.Holy, 8f, 25f, 1.0f, 7f, 45f, 1.2f, SkillBehaviorType.Summon),
                    Active("Level5_B", "부활", "쓰러진 아군을 부활시킨다.", 5, 300, 3,
                        DamageType.Holy, 30f, 50f, 3.0f, 5f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 30f, duration = 5f, tickInterval = 1f } },
                        statusDuration: 5f, statusChance: 100f),
                    Passive("Level5_C", "축복의 몸", "축복받은 몸으로 모든 저항이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { vitality = 5, magicDefense = 6, defense = 4, intelligence = 3 }, hp: 35f),

                    Active("Level7_A", "천사의 빛", "천사의 빛을 내려 적에게 큰 신성 피해를 준다.", 7, 500, 4,
                        DamageType.Holy, 10f, 35f, 1.5f, 9f, 70f, 1.4f, SkillBehaviorType.Summon),
                    Active("Level7_B", "집단 치유", "주변 모든 아군의 체력을 크게 회복한다.", 7, 500, 4,
                        DamageType.Holy, 12f, 45f, 2.0f, 10f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 25f, duration = 10f, tickInterval = 1f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level7_C", "대성직자", "대성직자의 힘으로 모든 치유와 신성 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 8, magicDefense = 8, vitality = 5 }, hp: 50f, mp: 60f),

                    Active("Ultimate", "천상의 심판", "하늘에서 천상의 빛을 내려 적에게 치명적 신성 피해를 주고 아군을 치유한다.", 10, 1000, 5,
                        DamageType.Holy, 30f, 65f, 2.5f, 12f, 140f, 1.8f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 15. Druid (드루이드) - 자연/변신 테마
        // ====================================================================
        private JobDefinition GetDruidSkills()
        {
            return new JobDefinition
            {
                jobName = "Druid",
                category = SkillCategory.Nature,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "가시 덩굴", "가시 덩굴을 소환하여 적을 속박하고 피해를 준다.", 1, 50, 1,
                        DamageType.Physical, 4f, 10f, 0.7f, 6f, 13f, 0.7f, SkillBehaviorType.Summon,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Root, value = 1f, duration = 2f } },
                        statusDuration: 2f, statusChance: 60f),
                    Active("Level1_B", "자연의 분노", "자연의 힘으로 적에게 피해를 준다.", 1, 50, 1,
                        DamageType.Physical, 3f, 8f, 0.6f, 7f, 15f, 0.9f, SkillBehaviorType.Projectile),
                    Passive("Level1_C", "자연 친화", "자연과 교감하여 체력 재생과 지능이 증가한다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 3, vitality = 2 }, hp: 10f),

                    Active("Level3_A", "야수 소환", "숲의 야수를 소환하여 적을 공격한다.", 3, 150, 2,
                        DamageType.Physical, 10f, 18f, 1.0f, 6f, 28f, 0.9f, SkillBehaviorType.Summon),
                    Active("Level3_B", "치유의 비", "치유의 비를 내려 주변 아군의 체력을 회복한다.", 3, 150, 2,
                        DamageType.Physical, 8f, 16f, 1.2f, 7f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Regeneration, value = 10f, duration = 6f, tickInterval = 1f } },
                        statusDuration: 6f, statusChance: 100f),
                    Passive("Level3_C", "대지의 은혜", "대지의 은혜로 방어력과 체력이 증가한다.", 3, 150, 2,
                        stats: new StatBlock { vitality = 5, defense = 3, intelligence = 2 }, hp: 25f),

                    Active("Level5_A", "폭풍 소환", "자연의 폭풍을 소환하여 범위 피해를 준다.", 5, 300, 3,
                        DamageType.Lightning, 9f, 26f, 1.2f, 8f, 45f, 1.1f, SkillBehaviorType.Summon),
                    Active("Level5_B", "곰 변신", "거대한 곰으로 변신하여 공격력과 방어력이 증가한다.", 5, 300, 3,
                        DamageType.Physical, 15f, 30f, 1.0f, 2f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Enhancement, value = 35f, duration = 12f } },
                        statusDuration: 12f, statusChance: 100f),
                    Passive("Level5_C", "숲의 수호자", "숲의 수호자로서 자연 능력이 강화된다.", 5, 300, 3,
                        stats: new StatBlock { intelligence = 5, vitality = 5, defense = 3 }, hp: 30f, mp: 25f),

                    Active("Level7_A", "세계수의 분노", "세계수의 힘을 빌려 적에게 강력한 자연의 피해를 준다.", 7, 500, 4,
                        DamageType.Physical, 12f, 38f, 1.5f, 9f, 80f, 1.4f, SkillBehaviorType.Summon),
                    Active("Level7_B", "늑대 변신", "흉포한 늑대로 변신하여 공격속도와 이동속도가 극대화된다.", 7, 500, 4,
                        DamageType.Physical, 18f, 30f, 0.5f, 2f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Speed, value = 50f, duration = 15f } },
                        statusDuration: 15f, statusChance: 100f),
                    Passive("Level7_C", "대드루이드", "대드루이드로서 자연의 모든 힘을 다룰 수 있다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 8, vitality = 6, defense = 4, magicDefense = 4 }, hp: 50f, mp: 40f),

                    Active("Ultimate", "세계수의 각성", "세계수를 각성시켜 대지의 힘으로 적을 분쇄하고 아군을 치유한다.", 10, 1000, 5,
                        DamageType.Physical, 30f, 60f, 2.0f, 10f, 145f, 1.8f, SkillBehaviorType.Summon),
                }
            };
        }

        // ====================================================================
        // 16. Amplifier (증폭술사) - 증폭/버프 테마
        // ====================================================================
        private JobDefinition GetAmplifierSkills()
        {
            return new JobDefinition
            {
                jobName = "Amplifier",
                category = SkillCategory.Spirit,
                skills = new SkillDefinition[]
                {
                    Active("Level1_A", "마력 증폭탄", "증폭된 마력을 발사하여 적에게 피해를 준다.", 1, 50, 1,
                        DamageType.Magical, 3f, 10f, 0.6f, 8f, 14f, 0.9f, SkillBehaviorType.Projectile),
                    Active("Level1_B", "능력 증폭", "자신의 능력을 일시적으로 증폭시킨다.", 1, 50, 1,
                        DamageType.Magical, 8f, 12f, 0.5f, 1f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Enhancement, value = 15f, duration = 8f } },
                        statusDuration: 8f, statusChance: 100f),
                    Passive("Level1_C", "증폭 기초", "증폭술의 기초로 지능과 마나가 증가한다.", 1, 50, 1,
                        stats: new StatBlock { intelligence = 4 }, mp: 20f),

                    Active("Level3_A", "증폭 폭발", "증폭된 에너지를 폭발시켜 범위 피해를 준다.", 3, 150, 2,
                        DamageType.Magical, 5f, 16f, 0.8f, 6f, 26f, 1.0f, SkillBehaviorType.Summon),
                    Active("Level3_B", "아군 강화", "아군의 전투 능력을 증폭시킨다.", 3, 150, 2,
                        DamageType.Magical, 10f, 20f, 1.0f, 8f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Strength, value = 20f, duration = 10f } },
                        statusDuration: 10f, statusChance: 100f),
                    Passive("Level3_C", "마나 증폭", "마나 효율이 증폭되어 마나가 크게 증가한다.", 3, 150, 2,
                        stats: new StatBlock { intelligence = 5, luck = 2 }, mp: 40f),

                    Active("Level5_A", "증폭 레이저", "증폭된 에너지를 레이저로 발사한다.", 5, 300, 3,
                        DamageType.Magical, 7f, 25f, 1.0f, 10f, 48f, 1.2f, SkillBehaviorType.Projectile),
                    Active("Level5_B", "전체 증폭", "주변 모든 아군의 능력을 대폭 증폭시킨다.", 5, 300, 3,
                        DamageType.Magical, 15f, 35f, 1.5f, 10f, 0f, 0f,
                        effects: new StatusEffect[] { new StatusEffect { type = StatusType.Blessing, value = 25f, duration = 12f } },
                        statusDuration: 12f, statusChance: 100f),
                    Passive("Level5_C", "증폭 마스터", "증폭술이 숙달되어 모든 마법 능력이 증가한다.", 5, 300, 3,
                        stats: new StatBlock { intelligence = 7, magicDefense = 4, luck = 3 }, mp: 50f),

                    Active("Level7_A", "차원 증폭", "차원의 에너지를 증폭시켜 폭발적 피해를 준다.", 7, 500, 4,
                        DamageType.Magical, 10f, 38f, 1.2f, 9f, 78f, 1.5f, SkillBehaviorType.Summon),
                    Active("Level7_B", "궁극 강화", "아군에게 궁극의 강화를 부여한다.", 7, 500, 4,
                        DamageType.Magical, 20f, 50f, 2.0f, 8f, 0f, 0f,
                        effects: new StatusEffect[] {
                            new StatusEffect { type = StatusType.Blessing, value = 40f, duration = 15f },
                            new StatusEffect { type = StatusType.Shield, value = 30f, duration = 10f } },
                        statusDuration: 15f, statusChance: 100f),
                    Passive("Level7_C", "대증폭술사", "증폭술의 경지에 올라 모든 능력이 극대화된다.", 7, 500, 4,
                        stats: new StatBlock { intelligence = 10, magicDefense = 6, luck = 5 }, mp: 70f),

                    Active("Ultimate", "무한 증폭", "에너지를 무한히 증폭시켜 주변의 모든 적을 소멸시킨다.", 10, 1000, 5,
                        DamageType.Magical, 30f, 70f, 2.5f, 12f, 155f, 2.0f, SkillBehaviorType.Summon),
                }
            };
        }
    }
}
