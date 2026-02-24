using UnityEngine;
using UnityEditor;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 패시브 스킬 트리 노드 80개 자동 생성
    /// 4종족 × 20노드 (Tier1:8소 + Tier2:6대 + Tier3:4특수 + Tier4:2키스톤)
    /// </summary>
    public class PassiveNodeDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/PassiveNodes";

        [MenuItem("Dungeon Crawler/Generate Passive Tree Nodes")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int total = 0;
            total += GenerateHumanNodes();    // 20
            total += GenerateElfNodes();      // 20
            total += GenerateBeastNodes();    // 20
            total += GenerateMachinaNodes();  // 20

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PassiveNodeGenerator] {total}개 패시브 노드 생성 완료");
        }

        // ===================== 인간 (균형형) =====================
        private static int GenerateHumanNodes()
        {
            Race race = Race.Human;
            string p = "human";
            int c = 0;

            // Tier1 - 소형 (8)
            c += N($"{p}_str1", "근력 단련 I", "기초 근력 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, str: 2);
            c += N($"{p}_agi1", "민첩 훈련 I", "기초 민첩 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, agi: 2);
            c += N($"{p}_vit1", "체력 단련 I", "기초 체력 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, vit: 2);
            c += N($"{p}_int1", "지력 수련 I", "기초 지력 수련.", race, PT.Minor, PTier.Tier1, 1, 1, intel: 2);
            c += N($"{p}_hp1", "생명력 증가 I", "HP +20.", race, PT.Minor, PTier.Tier1, 1, 1, hp: 20);
            c += N($"{p}_mp1", "마력 증가 I", "MP +15.", race, PT.Minor, PTier.Tier1, 1, 1, mp: 15);
            c += N($"{p}_def1", "방어 훈련 I", "기초 방어 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, def: 2);
            c += N($"{p}_crit1", "급소 파악 I", "크리티컬 확률 +1%.", race, PT.Minor, PTier.Tier1, 1, 1, critCh: 1);

            // Tier2 - 대형 (6)
            c += N($"{p}_str2", "근력 단련 II", "강화된 근력.", race, PT.Major, PTier.Tier2, 2, 3, str: 4, prereqs: $"{p}_str1");
            c += N($"{p}_agi2", "민첩 훈련 II", "강화된 민첩.", race, PT.Major, PTier.Tier2, 2, 3, agi: 4, prereqs: $"{p}_agi1");
            c += N($"{p}_vit2", "체력 단련 II", "강화된 체력.", race, PT.Major, PTier.Tier2, 2, 3, vit: 4, hp: 30, prereqs: $"{p}_vit1");
            c += N($"{p}_int2", "지력 수련 II", "강화된 지력.", race, PT.Major, PTier.Tier2, 2, 3, intel: 4, mp: 20, prereqs: $"{p}_int1");
            c += N($"{p}_allround", "만능 전사", "균형 잡힌 훈련.", race, PT.Major, PTier.Tier2, 2, 5, str: 2, agi: 2, vit: 2, intel: 2, prereqs: $"{p}_str1");
            c += N($"{p}_combat", "전투 본능", "크리 +2%, 공속 +3%.", race, PT.Major, PTier.Tier2, 2, 5, critCh: 2, atkSpd: 3, prereqs: $"{p}_crit1");

            // Tier3 - 특수 (4)
            c += N($"{p}_offense", "공격 달인", "STR+5, 크리데미지+10%.", race, PT.Major, PTier.Tier3, 3, 7, str: 5, critDmg: 10, prereqs: $"{p}_str2");
            c += N($"{p}_defense", "방어 달인", "VIT+5, DEF+5, HP+50.", race, PT.Major, PTier.Tier3, 3, 7, vit: 5, def: 5, hp: 50, prereqs: $"{p}_vit2");
            c += N($"{p}_speed", "속도의 정점", "AGI+5, 이속+5%, 쿨감+3%.", race, PT.Major, PTier.Tier3, 3, 7, agi: 5, moveSpd: 5, cdReduce: 3, prereqs: $"{p}_agi2");
            c += N($"{p}_magic", "마법 달인", "INT+5, MP+40, 마나재생+5.", race, PT.Major, PTier.Tier3, 3, 7, intel: 5, mp: 40, manaRegen: 5, prereqs: $"{p}_int2");

            // Tier4 - 키스톤 (2)
            c += N($"{p}_ks_jack", "만능인의 축복", "모든 스탯 +5. 크리티컬 불가.", race, PT.Keystone, PTier.Tier4, 4, 10,
                str: 5, agi: 5, vit: 5, intel: 5, def: 5, mdef: 5, luk: 5, stab: 5,
                isKS: true, ksType: PassiveKeystoneType.PerfectBalance, ksPos: 5, ksNeg: -100,
                ksPosDesc: "모든 스탯 +5", ksNegDesc: "크리티컬 확률 0% 고정",
                prereqs: $"{p}_allround");
            c += N($"{p}_ks_life", "생명 연결", "데미지 8% HP 회복. 최대HP -20%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                lifesteal: 8, hpPct: -20,
                isKS: true, ksType: PassiveKeystoneType.LifeLink, ksPos: 8, ksNeg: -20,
                ksPosDesc: "가한 데미지의 8% HP 회복", ksNegDesc: "최대 HP -20%",
                prereqs: $"{p}_defense");

            return c;
        }

        // ===================== 엘프 (마법 특화) =====================
        private static int GenerateElfNodes()
        {
            Race race = Race.Elf;
            string p = "elf";
            int c = 0;

            // Tier1
            c += N($"{p}_int1", "마력 각성 I", "마법 기초 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, intel: 3);
            c += N($"{p}_mdef1", "마법 저항 I", "마법 방어 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, mdef: 3);
            c += N($"{p}_mp1", "마나 확장 I", "MP +20.", race, PT.Minor, PTier.Tier1, 1, 1, mp: 20);
            c += N($"{p}_agi1", "바람 걸음 I", "민첩 훈련.", race, PT.Minor, PTier.Tier1, 1, 1, agi: 2);
            c += N($"{p}_mana_regen1", "마나 흐름 I", "마나 재생 +3.", race, PT.Minor, PTier.Tier1, 1, 1, manaRegen: 3);
            c += N($"{p}_cdr1", "주문 가속 I", "쿨다운 -2%.", race, PT.Minor, PTier.Tier1, 1, 1, cdReduce: 2);
            c += N($"{p}_critm1", "마법 집중 I", "마법 크리 +1%.", race, PT.Minor, PTier.Tier1, 1, 1, critCh: 1);
            c += N($"{p}_luk1", "정령의 축복 I", "LUK +2.", race, PT.Minor, PTier.Tier1, 1, 1, luk: 2);

            // Tier2
            c += N($"{p}_int2", "마력 각성 II", "강화된 마력.", race, PT.Major, PTier.Tier2, 2, 3, intel: 5, mp: 20, prereqs: $"{p}_int1");
            c += N($"{p}_mdef2", "마법 저항 II", "강화된 마방.", race, PT.Major, PTier.Tier2, 2, 3, mdef: 5, prereqs: $"{p}_mdef1");
            c += N($"{p}_cdr2", "주문 가속 II", "쿨감 -4%, 공속 +3%.", race, PT.Major, PTier.Tier2, 2, 5, cdReduce: 4, atkSpd: 3, prereqs: $"{p}_cdr1");
            c += N($"{p}_mana2", "마나 순환", "MP+30, 재생+5.", race, PT.Major, PTier.Tier2, 2, 5, mp: 30, manaRegen: 5, prereqs: $"{p}_mp1");
            c += N($"{p}_elem", "원소 친화", "INT+3, 크리+2%.", race, PT.Major, PTier.Tier2, 2, 5, intel: 3, critCh: 2, prereqs: $"{p}_critm1");
            c += N($"{p}_spirit", "정령 교감", "LUK+3, 이속+3%.", race, PT.Major, PTier.Tier2, 2, 5, luk: 3, moveSpd: 3, prereqs: $"{p}_luk1");

            // Tier3
            c += N($"{p}_archmage", "대마법사의 길", "INT+8, MP+50.", race, PT.Major, PTier.Tier3, 3, 7, intel: 8, mp: 50, prereqs: $"{p}_int2");
            c += N($"{p}_spellblade", "주술검사", "INT+4, STR+4, 크리데미지+8%.", race, PT.Major, PTier.Tier3, 3, 7, intel: 4, str: 4, critDmg: 8, prereqs: $"{p}_elem");
            c += N($"{p}_ward", "마법 보호", "MDEF+8, HP+30.", race, PT.Major, PTier.Tier3, 3, 7, mdef: 8, hp: 30, prereqs: $"{p}_mdef2");
            c += N($"{p}_tempest", "폭풍의 정령", "쿨감-6%, 마나재생+8.", race, PT.Major, PTier.Tier3, 3, 7, cdReduce: 6, manaRegen: 8, prereqs: $"{p}_cdr2");

            // Tier4 - 키스톤
            c += N($"{p}_ks_glass", "유리 대포", "모든 데미지 +40%. HP -30%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                intel: 10, critDmg: 20,
                isKS: true, ksType: PassiveKeystoneType.GlassCannon, ksPos: 40, ksNeg: -30,
                ksPosDesc: "모든 데미지 +40%", ksNegDesc: "최대 HP -30%",
                prereqs: $"{p}_archmage");
            c += N($"{p}_ks_blood", "피의 마법", "MP 대신 HP로 스킬 사용.", race, PT.Keystone, PTier.Tier4, 4, 10,
                hp: 100, manaRegen: -10,
                isKS: true, ksType: PassiveKeystoneType.BloodMagic, ksPos: 1, ksNeg: 0,
                ksPosDesc: "MP 대신 HP로 스킬 사용 가능", ksNegDesc: "MP 재생 불가",
                prereqs: $"{p}_mana2");

            return c;
        }

        // ===================== 수인 (물리 특화) =====================
        private static int GenerateBeastNodes()
        {
            Race race = Race.Beast;
            string p = "beast";
            int c = 0;

            // Tier1
            c += N($"{p}_str1", "야수의 힘 I", "원시적 근력.", race, PT.Minor, PTier.Tier1, 1, 1, str: 3);
            c += N($"{p}_agi1", "야생 본능 I", "야생의 민첩.", race, PT.Minor, PTier.Tier1, 1, 1, agi: 3);
            c += N($"{p}_vit1", "강인한 체력 I", "튼튼한 몸.", race, PT.Minor, PTier.Tier1, 1, 1, vit: 2, hp: 15);
            c += N($"{p}_crit1", "급소 감각 I", "크리 +2%.", race, PT.Minor, PTier.Tier1, 1, 1, critCh: 2);
            c += N($"{p}_atk_spd1", "연속 타격 I", "공속 +3%.", race, PT.Minor, PTier.Tier1, 1, 1, atkSpd: 3);
            c += N($"{p}_move1", "쾌속 이동 I", "이속 +3%.", race, PT.Minor, PTier.Tier1, 1, 1, moveSpd: 3);
            c += N($"{p}_stab1", "안정성 I", "STAB +2.", race, PT.Minor, PTier.Tier1, 1, 1, stab: 2);
            c += N($"{p}_life1", "생명력 흡수 I", "흡혈 1%.", race, PT.Minor, PTier.Tier1, 1, 1, lifesteal: 1);

            // Tier2
            c += N($"{p}_str2", "야수의 힘 II", "강화된 야수 힘.", race, PT.Major, PTier.Tier2, 2, 3, str: 5, critDmg: 5, prereqs: $"{p}_str1");
            c += N($"{p}_agi2", "야생 본능 II", "강화된 민첩+회피.", race, PT.Major, PTier.Tier2, 2, 3, agi: 5, moveSpd: 3, prereqs: $"{p}_agi1");
            c += N($"{p}_frenzy", "광폭화", "공속+5%, 크리+3%.", race, PT.Major, PTier.Tier2, 2, 5, atkSpd: 5, critCh: 3, prereqs: $"{p}_atk_spd1");
            c += N($"{p}_predator", "포식자", "흡혈+2%, STR+2.", race, PT.Major, PTier.Tier2, 2, 5, lifesteal: 2, str: 2, prereqs: $"{p}_life1");
            c += N($"{p}_tough", "강인함", "VIT+4, HP+40.", race, PT.Major, PTier.Tier2, 2, 5, vit: 4, hp: 40, prereqs: $"{p}_vit1");
            c += N($"{p}_hunter", "사냥꾼", "크리데미지+10%.", race, PT.Major, PTier.Tier2, 2, 5, critDmg: 10, prereqs: $"{p}_crit1");

            // Tier3
            c += N($"{p}_alpha", "알파 야수", "STR+8, 공속+5%.", race, PT.Major, PTier.Tier3, 3, 7, str: 8, atkSpd: 5, prereqs: $"{p}_str2");
            c += N($"{p}_shadow", "그림자 습격", "AGI+6, 크리+4%, 이속+5%.", race, PT.Major, PTier.Tier3, 3, 7, agi: 6, critCh: 4, moveSpd: 5, prereqs: $"{p}_agi2");
            c += N($"{p}_blood", "피의 갈증", "흡혈+4%, 크리데미지+15%.", race, PT.Major, PTier.Tier3, 3, 7, lifesteal: 4, critDmg: 15, prereqs: $"{p}_predator");
            c += N($"{p}_rage", "끝없는 분노", "공속+8%, 쿨감-5%.", race, PT.Major, PTier.Tier3, 3, 7, atkSpd: 8, cdReduce: 5, prereqs: $"{p}_frenzy");

            // Tier4 - 키스톤
            c += N($"{p}_ks_berserk", "광전사의 혼", "HP 낮을수록 데미지 증가(최대+50%). DEF-30%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                str: 8, critDmg: 20,
                isKS: true, ksType: PassiveKeystoneType.Berserker, ksPos: 50, ksNeg: -30,
                ksPosDesc: "HP 50% 이하 시 데미지 최대 +50%", ksNegDesc: "DEF -30%",
                prereqs: $"{p}_alpha");
            c += N($"{p}_ks_soul", "영혼 포식", "영혼 획득률 +100%. EXP -25%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                lifesteal: 5, luk: 5,
                isKS: true, ksType: PassiveKeystoneType.SoulAbsorption, ksPos: 100, ksNeg: -25,
                ksPosDesc: "영혼 획득률 +100%", ksNegDesc: "경험치 획득 -25%",
                prereqs: $"{p}_blood");

            return c;
        }

        // ===================== 기계족 (방어 특화) =====================
        private static int GenerateMachinaNodes()
        {
            Race race = Race.Machina;
            string p = "machina";
            int c = 0;

            // Tier1
            c += N($"{p}_def1", "합금 장갑 I", "기본 방어 강화.", race, PT.Minor, PTier.Tier1, 1, 1, def: 3);
            c += N($"{p}_mdef1", "마법 차폐 I", "마법 방어 강화.", race, PT.Minor, PTier.Tier1, 1, 1, mdef: 3);
            c += N($"{p}_hp1", "내구도 증가 I", "HP +25.", race, PT.Minor, PTier.Tier1, 1, 1, hp: 25);
            c += N($"{p}_stab1", "안정 장치 I", "STAB +3.", race, PT.Minor, PTier.Tier1, 1, 1, stab: 3);
            c += N($"{p}_vit1", "프레임 강화 I", "VIT +2.", race, PT.Minor, PTier.Tier1, 1, 1, vit: 2);
            c += N($"{p}_str1", "동력 장치 I", "STR +2.", race, PT.Minor, PTier.Tier1, 1, 1, str: 2);
            c += N($"{p}_int1", "연산 장치 I", "INT +2.", race, PT.Minor, PTier.Tier1, 1, 1, intel: 2);
            c += N($"{p}_cdr1", "냉각 시스템 I", "쿨감 -2%.", race, PT.Minor, PTier.Tier1, 1, 1, cdReduce: 2);

            // Tier2
            c += N($"{p}_def2", "합금 장갑 II", "강화 방어.", race, PT.Major, PTier.Tier2, 2, 3, def: 6, prereqs: $"{p}_def1");
            c += N($"{p}_mdef2", "마법 차폐 II", "강화 마방.", race, PT.Major, PTier.Tier2, 2, 3, mdef: 6, prereqs: $"{p}_mdef1");
            c += N($"{p}_hp2", "내구도 증가 II", "HP+50, VIT+3.", race, PT.Major, PTier.Tier2, 2, 5, hp: 50, vit: 3, prereqs: $"{p}_hp1");
            c += N($"{p}_engine", "고출력 엔진", "STR+4, 공속+3%.", race, PT.Major, PTier.Tier2, 2, 5, str: 4, atkSpd: 3, prereqs: $"{p}_str1");
            c += N($"{p}_compute", "고급 연산", "INT+4, 쿨감-3%.", race, PT.Major, PTier.Tier2, 2, 5, intel: 4, cdReduce: 3, prereqs: $"{p}_int1");
            c += N($"{p}_stable", "초안정 시스템", "STAB+5, DEF+3.", race, PT.Major, PTier.Tier2, 2, 5, stab: 5, def: 3, prereqs: $"{p}_stab1");

            // Tier3
            c += N($"{p}_fortress", "이동 요새", "DEF+10, HP+60.", race, PT.Major, PTier.Tier3, 3, 7, def: 10, hp: 60, prereqs: $"{p}_def2");
            c += N($"{p}_aegis", "에이지스 방벽", "MDEF+10, MP+30.", race, PT.Major, PTier.Tier3, 3, 7, mdef: 10, mp: 30, prereqs: $"{p}_mdef2");
            c += N($"{p}_overdrive", "오버드라이브", "STR+6, 공속+5%, 크리+3%.", race, PT.Major, PTier.Tier3, 3, 7, str: 6, atkSpd: 5, critCh: 3, prereqs: $"{p}_engine");
            c += N($"{p}_adaptive", "적응형 장갑", "DEF+5, MDEF+5, HP%+5%.", race, PT.Major, PTier.Tier3, 3, 7, def: 5, mdef: 5, hpPct: 5, prereqs: $"{p}_hp2");

            // Tier4 - 키스톤
            c += N($"{p}_ks_iron", "철의 요새", "DEF +30. 이동속도 -25%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                def: 15, vit: 8, hp: 80,
                isKS: true, ksType: PassiveKeystoneType.IronFortress, ksPos: 30, ksNeg: -25,
                ksPosDesc: "DEF +30", ksNegDesc: "이동속도 -25%",
                prereqs: $"{p}_fortress");
            c += N($"{p}_ks_overload", "원소 과부하", "원소 데미지 +35%. 물리 데미지 -20%.", race, PT.Keystone, PTier.Tier4, 4, 10,
                intel: 8, mdef: 5,
                isKS: true, ksType: PassiveKeystoneType.ElementalOverload, ksPos: 35, ksNeg: -20,
                ksPosDesc: "원소 데미지 +35%", ksNegDesc: "물리 데미지 -20%",
                prereqs: $"{p}_compute");

            return c;
        }

        // ===================== 축약 타입 =====================
        private enum PT { Minor, Major, Keystone }
        private enum PTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4 }

        // ===================== 통합 생성 메서드 =====================
        private static int N(string id, string name, string desc,
            Race race, PT type, PTier tier, int cost, int reqLevel,
            float str = 0, float agi = 0, float vit = 0, float intel = 0,
            float def = 0, float mdef = 0, float luk = 0, float stab = 0,
            float hp = 0, float mp = 0, float hpPct = 0, float mpPct = 0,
            float critCh = 0, float critDmg = 0, float moveSpd = 0, float atkSpd = 0,
            float cdReduce = 0, float lifesteal = 0, float manaRegen = 0,
            bool isKS = false, PassiveKeystoneType ksType = PassiveKeystoneType.None,
            float ksPos = 0, float ksNeg = 0,
            string ksPosDesc = "", string ksNegDesc = "",
            string prereqs = "")
        {
            string assetPath = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<PassiveSkillTreeData>(assetPath) != null) return 0;

            var data = ScriptableObject.CreateInstance<PassiveSkillTreeData>();
            var so = new SerializedObject(data);

            so.FindProperty("nodeId").stringValue = id;
            so.FindProperty("nodeName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("requiredRace").enumValueIndex = (int)race;
            so.FindProperty("nodeType").enumValueIndex = (int)type;
            so.FindProperty("tier").enumValueIndex = (int)tier - 1; // enum 0-based
            so.FindProperty("pointCost").intValue = cost;
            so.FindProperty("requiredLevel").intValue = reqLevel;

            // 선행 노드
            if (!string.IsNullOrEmpty(prereqs))
            {
                var arr = prereqs.Split(',');
                var prop = so.FindProperty("prerequisiteNodeIds");
                prop.arraySize = arr.Length;
                for (int i = 0; i < arr.Length; i++)
                    prop.GetArrayElementAtIndex(i).stringValue = arr[i].Trim();
            }

            // 스탯 보너스
            var sb = so.FindProperty("statBonus");
            sb.FindPropertyRelative("strength").floatValue = str;
            sb.FindPropertyRelative("agility").floatValue = agi;
            sb.FindPropertyRelative("vitality").floatValue = vit;
            sb.FindPropertyRelative("intelligence").floatValue = intel;
            sb.FindPropertyRelative("defense").floatValue = def;
            sb.FindPropertyRelative("magicDefense").floatValue = mdef;
            sb.FindPropertyRelative("luck").floatValue = luk;
            sb.FindPropertyRelative("stability").floatValue = stab;

            so.FindProperty("hpBonus").floatValue = hp;
            so.FindProperty("mpBonus").floatValue = mp;
            so.FindProperty("hpPercentBonus").floatValue = hpPct;
            so.FindProperty("mpPercentBonus").floatValue = mpPct;
            so.FindProperty("critChanceBonus").floatValue = critCh;
            so.FindProperty("critDamageBonus").floatValue = critDmg;
            so.FindProperty("moveSpeedBonus").floatValue = moveSpd;
            so.FindProperty("attackSpeedBonus").floatValue = atkSpd;
            so.FindProperty("cooldownReduction").floatValue = cdReduce;
            so.FindProperty("lifestealPercent").floatValue = lifesteal;
            so.FindProperty("manaRegenBonus").floatValue = manaRegen;

            // 키스톤
            so.FindProperty("isKeystone").boolValue = isKS;
            so.FindProperty("keystoneType").enumValueIndex = (int)ksType;
            so.FindProperty("keystonePositiveValue").floatValue = ksPos;
            so.FindProperty("keystoneNegativeValue").floatValue = ksNeg;
            so.FindProperty("keystonePositiveDesc").stringValue = ksPosDesc;
            so.FindProperty("keystoneNegativeDesc").stringValue = ksNegDesc;

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(data, assetPath);
            return 1;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
