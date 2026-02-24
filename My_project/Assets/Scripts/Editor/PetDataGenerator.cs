using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 펫 데이터 15종 자동 생성
    /// 전투형 5 + 수집형 5 + 버프형 5
    /// </summary>
    public class PetDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/PetData";

        [MenuItem("Dungeon Crawler/Generate Pet Data")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int total = 0;
            total += GenerateCombatPets();    // 5
            total += GenerateCollectorPets(); // 5
            total += GenerateBufferPets();    // 5

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PetDataGenerator] {total}개 펫 생성 완료");
        }

        // ===================== 전투형 펫 5종 =====================
        private static int GenerateCombatPets()
        {
            int count = 0;

            count += CreatePet("pet_wolf", "늑대", "충성스러운 늑대. 적을 물어뜯어 공격한다.",
                PetType.Combat, PetRarity.Common, PetElement.None,
                80, 12, 6, 4f, 8f, 2f, 1f, 30, 40,
                damageType: DamageType.Physical, atkRange: 2f, atkCd: 1.5f,
                price: 500, dropSrc: "야수 몬스터 처치",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("물기", "적을 강하게 물어뜯는다.", 1, PetAbilityType.AttackBoost, 10f, 5f),
                    new PetAbilityDef("울부짖기", "울부짖어 적을 위협한다.", 10, PetAbilityType.Taunt, 0, 15f),
                    new PetAbilityDef("돌진", "적에게 돌진하여 큰 피해를 준다.", 20, PetAbilityType.AttackBoost, 25f, 10f)
                });

            count += CreatePet("pet_fire_imp", "화염 임프", "작지만 강력한 화염 정령. 불꽃을 뿜어 공격한다.",
                PetType.Combat, PetRarity.Uncommon, PetElement.Fire,
                60, 18, 4, 3.5f, 6f, 3f, 0.5f, 30, 50,
                damageType: DamageType.Fire, atkRange: 4f, atkCd: 2f,
                price: 1500, dropSrc: "화염 원소 몬스터 처치",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("화염탄", "불꽃 구체를 발사한다.", 1, PetAbilityType.ElementalAttack, 15f, 3f),
                    new PetAbilityDef("화염 폭발", "주변에 화염 폭발을 일으킨다.", 15, PetAbilityType.ElementalAttack, 30f, 12f),
                    new PetAbilityDef("불꽃 방패", "주인에게 화염 보호막을 씌운다.", 25, PetAbilityType.Shield, 50f, 20f)
                },
                evolveTo: "pet_fire_phoenix", evolveLevel: 30, evolveMat: "mat_fire_essence", evolveMatCount: 10);

            count += CreatePet("pet_ice_golem", "얼음 골렘", "차가운 얼음으로 만들어진 골렘. 튼튼한 방어와 느린 공격.",
                PetType.Combat, PetRarity.Rare, PetElement.Ice,
                150, 10, 15, 2.5f, 15f, 1.5f, 2f, 30, 60,
                damageType: DamageType.Ice, atkRange: 1.5f, atkCd: 2.5f,
                price: 3000, dropSrc: "얼음 원소 보스 처치",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("얼음 주먹", "얼음 주먹으로 강하게 내리친다.", 1, PetAbilityType.AttackBoost, 12f, 4f),
                    new PetAbilityDef("빙결 갑옷", "방어력이 크게 증가한다.", 10, PetAbilityType.DefenseBoost, 20f, 15f),
                    new PetAbilityDef("서리 폭발", "주변 적을 냉기로 둔화시킨다.", 20, PetAbilityType.ElementalAttack, 25f, 18f)
                });

            count += CreatePet("pet_shadow_cat", "그림자 고양이", "어둠 속에서 나타나는 신비한 고양이. 빠른 연속 공격.",
                PetType.Combat, PetRarity.Epic, PetElement.Dark,
                70, 22, 5, 6f, 7f, 3.5f, 0.8f, 30, 80,
                damageType: DamageType.Dark, atkRange: 2f, atkCd: 0.8f,
                price: 8000, dropSrc: "야간 이벤트 보스 처치",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("그림자 할퀴기", "그림자 발톱으로 빠르게 공격한다.", 1, PetAbilityType.AttackBoost, 15f, 2f),
                    new PetAbilityDef("은신", "잠시 투명해져 회피한다.", 15, PetAbilityType.Shield, 0, 20f),
                    new PetAbilityDef("암흑 질주", "적에게 돌진하며 암흑 피해.", 25, PetAbilityType.ElementalAttack, 40f, 12f)
                });

            count += CreatePet("pet_fire_phoenix", "화염 피닉스", "임프가 진화한 전설의 불사조. 부활 능력 보유.",
                PetType.Combat, PetRarity.Legendary, PetElement.Fire,
                120, 30, 10, 5f, 12f, 4f, 1.5f, 30, 100,
                damageType: DamageType.Fire, atkRange: 5f, atkCd: 1.5f,
                price: 0, dropSrc: "화염 임프 진화 (Lv.30 + 화염 정수 10개)",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("피닉스 화염", "강력한 화염으로 적을 불태운다.", 1, PetAbilityType.ElementalAttack, 35f, 3f),
                    new PetAbilityDef("재생의 불꽃", "주인의 HP를 회복시킨다.", 10, PetAbilityType.HealOwner, 50f, 15f),
                    new PetAbilityDef("불사", "주인이 죽으면 부활시킨다 (1회).", 25, PetAbilityType.Revive, 1f, 300f)
                });

            return count;
        }

        // ===================== 수집형 펫 5종 =====================
        private static int GenerateCollectorPets()
        {
            int count = 0;

            count += CreatePet("pet_fairy", "요정", "작고 귀여운 요정. 골드를 자동으로 모아준다.",
                PetType.Collector, PetRarity.Common, PetElement.Holy,
                30, 2, 2, 5f, 3f, 0.5f, 0.3f, 20, 30,
                pickupRadius: 4f, pickupGold: true, pickupItems: false, pickupCd: 0.3f,
                bonusGold: 5f,
                price: 300, dropSrc: "상점 구매",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("골드 센서", "골드 탐지 범위 증가.", 1, PetAbilityType.PickupRange, 1f, 0),
                    new PetAbilityDef("골드 배율", "골드 획득 +10%.", 10, PetAbilityType.GoldBoost, 10f, 0)
                });

            count += CreatePet("pet_treasure_slime", "보물 슬라임", "반짝이는 슬라임. 아이템과 골드를 모두 모아준다.",
                PetType.Collector, PetRarity.Uncommon, PetElement.None,
                50, 3, 3, 4f, 5f, 0.5f, 0.5f, 25, 45,
                pickupRadius: 5f, pickupGold: true, pickupItems: true, pickupCd: 0.5f,
                bonusGold: 8f, bonusDrop: 3f,
                price: 1000, dropSrc: "트레저 고블린 이벤트",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("보물 감지", "아이템 탐지 범위 증가.", 1, PetAbilityType.PickupRange, 2f, 0),
                    new PetAbilityDef("행운", "드롭률 +5%.", 12, PetAbilityType.DropBoost, 5f, 0),
                    new PetAbilityDef("보물 축복", "골드 +15%.", 20, PetAbilityType.GoldBoost, 15f, 0)
                });

            count += CreatePet("pet_magnet_sprite", "자석 정령", "자력으로 아이템을 끌어당기는 정령.",
                PetType.Collector, PetRarity.Rare, PetElement.Lightning,
                40, 5, 4, 5.5f, 4f, 0.8f, 0.5f, 25, 55,
                pickupRadius: 7f, pickupGold: true, pickupItems: true, pickupCd: 0.3f,
                bonusGold: 10f, bonusDrop: 5f,
                price: 2500, dropSrc: "원소 균열 이벤트",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("자력 강화", "줍기 범위 대폭 증가.", 1, PetAbilityType.PickupRange, 3f, 0),
                    new PetAbilityDef("행운의 자석", "드롭률 +8%.", 15, PetAbilityType.DropBoost, 8f, 0),
                    new PetAbilityDef("자력 폭풍", "넓은 범위 아이템 흡수.", 25, PetAbilityType.PickupRange, 5f, 0)
                });

            count += CreatePet("pet_mimic", "미믹", "보물 상자로 위장한 생물. 뛰어난 수집 능력.",
                PetType.Collector, PetRarity.Epic, PetElement.Dark,
                80, 8, 6, 3.5f, 8f, 1f, 0.8f, 30, 70,
                pickupRadius: 8f, pickupGold: true, pickupItems: true, pickupCd: 0.2f,
                bonusGold: 15f, bonusDrop: 8f,
                price: 5000, dropSrc: "던전 보물방 이벤트",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("탐욕", "골드 획득 +20%.", 1, PetAbilityType.GoldBoost, 20f, 0),
                    new PetAbilityDef("보물 사냥꾼", "드롭률 +12%.", 15, PetAbilityType.DropBoost, 12f, 0),
                    new PetAbilityDef("전리품 복제", "가끔 아이템 2배.", 25, PetAbilityType.DropBoost, 20f, 0)
                });

            count += CreatePet("pet_golden_dragon_baby", "아기 황금용", "황금빛 아기 드래곤. 최고의 수집 능력.",
                PetType.Collector, PetRarity.Legendary, PetElement.Fire,
                100, 12, 8, 5f, 10f, 1.5f, 1f, 30, 100,
                pickupRadius: 10f, pickupGold: true, pickupItems: true, pickupCd: 0.1f,
                bonusGold: 25f, bonusDrop: 15f,
                price: 0, dropSrc: "드래곤 습격 이벤트 보스 처치 (0.5%)",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("황금 축복", "골드 +30%.", 1, PetAbilityType.GoldBoost, 30f, 0),
                    new PetAbilityDef("드래곤의 눈", "드롭률 +20%.", 15, PetAbilityType.DropBoost, 20f, 0),
                    new PetAbilityDef("보물 감정", "경험치 +10%.", 25, PetAbilityType.ExpBoost, 10f, 0)
                });

            return count;
        }

        // ===================== 버프형 펫 5종 =====================
        private static int GenerateBufferPets()
        {
            int count = 0;

            count += CreatePet("pet_spirit_owl", "영혼 올빼미", "지혜의 영혼. INT와 경험치 보너스.",
                PetType.Buffer, PetRarity.Common, PetElement.Holy,
                40, 3, 3, 4f, 4f, 0.5f, 0.5f, 20, 30,
                bonusExp: 8f,
                statBonus: new float[] { 0, 0, 0, 3, 0, 2, 1, 0 }, // INT+3, MDEF+2, LUK+1
                price: 400, dropSrc: "상점 구매",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("지혜의 빛", "INT +2 추가.", 1, PetAbilityType.AttackBoost, 2f, 0),
                    new PetAbilityDef("지식 흡수", "경험치 +5%.", 10, PetAbilityType.ExpBoost, 5f, 0)
                });

            count += CreatePet("pet_war_horn", "전쟁의 뿔피리", "전장의 사기를 올리는 정령. STR/AGI 보너스.",
                PetType.Buffer, PetRarity.Uncommon, PetElement.None,
                60, 5, 5, 3.5f, 6f, 0.8f, 0.5f, 25, 45,
                statBonus: new float[] { 3, 2, 0, 0, 0, 0, 0, 1 }, // STR+3, AGI+2, STAB+1
                price: 1200, dropSrc: "오크 전쟁 부대 이벤트",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("전투의 함성", "STR +3 추가.", 1, PetAbilityType.AttackBoost, 3f, 0),
                    new PetAbilityDef("전장의 기운", "AGI +2 추가.", 12, PetAbilityType.AttackBoost, 2f, 0),
                    new PetAbilityDef("승리의 울림", "이동속도 증가.", 20, PetAbilityType.Sprint, 15f, 30f)
                });

            count += CreatePet("pet_guardian_angel", "수호 천사", "주인을 보호하는 천사. VIT/DEF 보너스 + 힐.",
                PetType.Buffer, PetRarity.Rare, PetElement.Holy,
                80, 4, 10, 4f, 8f, 0.5f, 1.5f, 25, 60,
                statBonus: new float[] { 0, 0, 4, 0, 3, 3, 0, 0 }, // VIT+4, DEF+3, MDEF+3
                price: 3000, dropSrc: "유령 조우 이벤트",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("수호의 빛", "DEF +3 추가.", 1, PetAbilityType.DefenseBoost, 3f, 0),
                    new PetAbilityDef("치유의 기도", "주인 HP 회복.", 12, PetAbilityType.HealOwner, 30f, 20f),
                    new PetAbilityDef("성스러운 보호막", "보호막 생성.", 22, PetAbilityType.Shield, 60f, 30f)
                });

            count += CreatePet("pet_fortune_cat", "행운의 고양이", "부와 행운을 가져다주는 고양이. LUK/골드 보너스.",
                PetType.Buffer, PetRarity.Epic, PetElement.None,
                50, 6, 4, 5f, 5f, 1f, 0.5f, 30, 75,
                bonusGold: 20f, bonusExp: 5f, bonusDrop: 10f,
                statBonus: new float[] { 0, 0, 0, 0, 0, 0, 5, 0 }, // LUK+5
                price: 6000, dropSrc: "도박사 이벤트 (행운 시)",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("행운의 손짓", "LUK +3 추가.", 1, PetAbilityType.AttackBoost, 3f, 0),
                    new PetAbilityDef("황금 비", "골드 +15%.", 15, PetAbilityType.GoldBoost, 15f, 0),
                    new PetAbilityDef("기적의 발", "드롭률 +15%.", 25, PetAbilityType.DropBoost, 15f, 0)
                });

            count += CreatePet("pet_ancient_spirit", "고대 정령", "고대의 힘을 가진 정령. 모든 스탯 보너스.",
                PetType.Buffer, PetRarity.Legendary, PetElement.None,
                120, 8, 8, 4.5f, 12f, 1.2f, 1.2f, 30, 100,
                bonusGold: 10f, bonusExp: 10f, bonusDrop: 10f,
                statBonus: new float[] { 3, 3, 3, 3, 3, 3, 3, 3 }, // 모든 스탯 +3
                price: 0, dropSrc: "수호자의 시련 이벤트 클리어 (1%)",
                abilities: new PetAbilityDef[] {
                    new PetAbilityDef("고대의 지혜", "모든 스탯 +2.", 1, PetAbilityType.AttackBoost, 2f, 0),
                    new PetAbilityDef("고대의 치유", "주인 HP 회복.", 10, PetAbilityType.HealOwner, 50f, 15f),
                    new PetAbilityDef("고대의 보호", "보호막 생성.", 20, PetAbilityType.Shield, 80f, 25f),
                    new PetAbilityDef("불사의 축복", "부활 1회.", 30, PetAbilityType.Revive, 1f, 300f)
                });

            return count;
        }

        // ===================== 헬퍼 =====================

        private struct PetAbilityDef
        {
            public string name, desc;
            public int level;
            public PetAbilityType type;
            public float value, cooldown;
            public PetAbilityDef(string n, string d, int l, PetAbilityType t, float v, float cd)
            { name = n; desc = d; level = l; type = t; value = v; cooldown = cd; }
        }

        private static int CreatePet(string id, string name, string desc,
            PetType type, PetRarity rarity, PetElement element,
            int hp, int atk, int def, float speed,
            float hpGr, float atkGr, float defGr, int maxLv, int baseExp,
            DamageType damageType = DamageType.Physical, float atkRange = 2f, float atkCd = 1.5f,
            float pickupRadius = 3f, bool pickupGold = true, bool pickupItems = false, float pickupCd = 0.5f,
            float bonusGold = 0, float bonusExp = 0, float bonusDrop = 0,
            float[] statBonus = null, long price = 0, string dropSrc = "",
            PetAbilityDef[] abilities = null,
            string evolveTo = "", int evolveLevel = 0, string evolveMat = "", int evolveMatCount = 0)
        {
            string assetPath = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<PetData>(assetPath) != null) return 0;

            var data = ScriptableObject.CreateInstance<PetData>();
            var so = new SerializedObject(data);

            so.FindProperty("petId").stringValue = id;
            so.FindProperty("petName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("petType").enumValueIndex = (int)type;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("element").enumValueIndex = (int)element;
            so.FindProperty("baseHP").intValue = hp;
            so.FindProperty("baseATK").intValue = atk;
            so.FindProperty("baseDEF").intValue = def;
            so.FindProperty("baseMoveSpeed").floatValue = speed;
            so.FindProperty("hpGrowth").floatValue = hpGr;
            so.FindProperty("atkGrowth").floatValue = atkGr;
            so.FindProperty("defGrowth").floatValue = defGr;
            so.FindProperty("maxLevel").intValue = maxLv;
            so.FindProperty("baseExpRequired").intValue = baseExp;
            so.FindProperty("damageType").enumValueIndex = (int)damageType;
            so.FindProperty("attackRange").floatValue = atkRange;
            so.FindProperty("attackCooldown").floatValue = atkCd;
            so.FindProperty("autoPickupRadius").floatValue = pickupRadius;
            so.FindProperty("canPickupGold").boolValue = pickupGold;
            so.FindProperty("canPickupItems").boolValue = pickupItems;
            so.FindProperty("pickupCooldown").floatValue = pickupCd;
            so.FindProperty("bonusGoldRate").floatValue = bonusGold;
            so.FindProperty("bonusExpRate").floatValue = bonusExp;
            so.FindProperty("bonusDropRate").floatValue = bonusDrop;
            so.FindProperty("purchasePrice").longValue = price;
            so.FindProperty("dropSource").stringValue = dropSrc;
            so.FindProperty("evolveToPetId").stringValue = evolveTo ?? "";
            so.FindProperty("evolveLevel").intValue = evolveLevel;
            so.FindProperty("evolveMaterialId").stringValue = evolveMat ?? "";
            so.FindProperty("evolveMaterialCount").intValue = evolveMatCount;

            // 스탯 보너스
            if (statBonus != null && statBonus.Length >= 8)
            {
                var sb = so.FindProperty("ownerStatBonus");
                sb.FindPropertyRelative("strength").floatValue = statBonus[0];
                sb.FindPropertyRelative("agility").floatValue = statBonus[1];
                sb.FindPropertyRelative("vitality").floatValue = statBonus[2];
                sb.FindPropertyRelative("intelligence").floatValue = statBonus[3];
                sb.FindPropertyRelative("defense").floatValue = statBonus[4];
                sb.FindPropertyRelative("magicDefense").floatValue = statBonus[5];
                sb.FindPropertyRelative("luck").floatValue = statBonus[6];
                sb.FindPropertyRelative("stability").floatValue = statBonus[7];
            }

            // 능력 배열
            if (abilities != null)
            {
                var abProp = so.FindProperty("abilities");
                abProp.arraySize = abilities.Length;
                for (int i = 0; i < abilities.Length; i++)
                {
                    var elem = abProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("abilityName").stringValue = abilities[i].name;
                    elem.FindPropertyRelative("abilityDescription").stringValue = abilities[i].desc;
                    elem.FindPropertyRelative("unlockLevel").intValue = abilities[i].level;
                    elem.FindPropertyRelative("abilityType").enumValueIndex = (int)abilities[i].type;
                    elem.FindPropertyRelative("value").floatValue = abilities[i].value;
                    elem.FindPropertyRelative("cooldown").floatValue = abilities[i].cooldown;
                }
            }

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
