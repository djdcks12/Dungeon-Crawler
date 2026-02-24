using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 던전 이벤트 데이터 60종+ 자동 생성
    /// 제단10 + 샘물5 + 큐리오15 + 보물방5 + 함정8 + 아레나5 + 시련5 + 상인3 + 포탈4 + 휴식3 + 기타5
    /// </summary>
    public class DungeonEventDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/DungeonEvents";

        [MenuItem("Dungeon Crawler/Generate Dungeon Event Data")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int total = 0;
            total += GenerateShrineEvents();      // 10
            total += GenerateFountainEvents();    // 5
            total += GenerateCurioEvents();       // 15
            total += GenerateTreasureEvents();    // 5
            total += GenerateAmbushEvents();      // 8
            total += GenerateArenaEvents();       // 5
            total += GenerateTrialEvents();       // 5
            total += GenerateShopEvents();        // 3
            total += GeneratePortalEvents();      // 4
            total += GenerateRestEvents();        // 3

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DungeonEventDataGenerator] {total}개 이벤트 생성 완료");
        }

        // ===================== 제단 이벤트 10종 =====================
        private static int GenerateShrineEvents()
        {
            int count = 0;

            // 보호의 제단
            count += CreateEvent("shrine_protection", "보호의 제단", "신성한 빛이 감싸는 제단. 기도하면 피해를 줄여준다.",
                DungeonEventType.Shrine, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 10, "기도한다",
                new List<EventOutcome> {
                    Outcome("보호의 축복을 받았다!", 1f, EventEffectType.ReduceDamage, 25f, 120f),
                }) ? 1 : 0;

            // 힘의 제단
            count += CreateEvent("shrine_power", "힘의 제단", "화염이 타오르는 전투의 제단. 공격력이 증가한다.",
                DungeonEventType.Shrine, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 10, "손을 올린다",
                new List<EventOutcome> {
                    Outcome("전투의 축복을 받았다!", 1f, EventEffectType.IncreaseDamage, 50f, 120f),
                }) ? 1 : 0;

            // 속도의 제단
            count += CreateEvent("shrine_speed", "속도의 제단", "바람이 소용돌이치는 제단. 이동이 빨라진다.",
                DungeonEventType.Shrine, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 1, 10, "바람을 만진다",
                new List<EventOutcome> {
                    Outcome("바람의 축복을 받았다!", 1f, EventEffectType.IncreaseSpeed, 25f, 120f),
                }) ? 1 : 0;

            // 행운의 제단
            count += CreateEvent("shrine_fortune", "행운의 제단", "금빛으로 빛나는 제단. 보상이 늘어난다.",
                DungeonEventType.Shrine, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 10, "동전을 올린다",
                new List<EventOutcome> {
                    Outcome("행운의 축복을 받았다!", 1f, EventEffectType.GainGold, 25f, 120f),
                }) ? 1 : 0;

            // 광분의 제단
            count += CreateEvent("shrine_frenzy", "광분의 제단", "붉은 에너지가 소용돌이치는 제단. 공격 속도가 폭증한다.",
                DungeonEventType.Shrine, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 3, 10, "피를 바른다",
                new List<EventOutcome> {
                    Outcome("광분의 축복을 받았다!", 1f, EventEffectType.BuffStat, 25f, 120f),
                }) ? 1 : 0;

            // 깨달음의 제단
            count += CreateEvent("shrine_enlightenment", "깨달음의 제단", "고요한 빛을 내뿜는 지혜의 제단.",
                DungeonEventType.Shrine, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 1, 10, "명상한다",
                new List<EventOutcome> {
                    Outcome("깨달음의 축복을 받았다!", 1f, EventEffectType.GainExp, 25f, 120f),
                }) ? 1 : 0;

            // 집중의 제단 (상위)
            count += CreateEvent("shrine_channeling", "집중의 제단", "수정으로 된 거대한 제단. 마법 효율이 극대화된다.",
                DungeonEventType.Shrine, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.04f, 4, 10, "집중한다",
                new List<EventOutcome> {
                    Outcome("집중의 힘이 깃들었다! 쿨다운 75% 감소!", 1f, EventEffectType.CooldownReset, 75f, 30f),
                }) ? 1 : 0;

            // 번개의 제단 (상위)
            count += CreateEvent("shrine_conduit", "번개의 제단", "전류가 흐르는 위험한 제단. 주변 적을 감전시킨다.",
                DungeonEventType.Shrine, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 5, 10, "제단을 만진다",
                new List<EventOutcome> {
                    Outcome("번개의 힘이 몸을 감싼다!", 1f, EventEffectType.IncreaseDamage, 500f, 30f),
                }) ? 1 : 0;

            // 무적의 제단 (상위)
            count += CreateEvent("shrine_invincibility", "무적의 제단", "빛의 장벽으로 둘러싸인 신성한 제단.",
                DungeonEventType.Shrine, DungeonEventRarity.Epic,
                DungeonEventTrigger.ChanceBased, 0.02f, 6, 10, "빛 속으로 들어간다",
                new List<EventOutcome> {
                    Outcome("무적의 축복! 모든 피해를 무시한다!", 1f, EventEffectType.InvincibilityShort, 100f, 30f),
                }) ? 1 : 0;

            // 강화의 제단 (상위)
            count += CreateEvent("shrine_empowered", "강화의 제단", "모든 원소의 에너지가 집결된 고대 제단.",
                DungeonEventType.Shrine, DungeonEventRarity.Epic,
                DungeonEventTrigger.ChanceBased, 0.02f, 7, 10, "힘을 흡수한다",
                new List<EventOutcome> {
                    Outcome("모든 능력이 강화되었다!", 1f, EventEffectType.BuffStat, 200f, 30f),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 샘물 이벤트 5종 =====================
        private static int GenerateFountainEvents()
        {
            int count = 0;

            count += CreateEvent("fountain_health", "치유의 샘", "맑은 물이 솟아오르는 신비한 샘. HP를 회복해준다.",
                DungeonEventType.Fountain, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.10f, 1, 10, "물을 마신다",
                new List<EventOutcome> {
                    Outcome("상처가 치유되었다!", 0.8f, EventEffectType.HealPercent, 50f, 0f),
                    Outcome("물이 오염되어 있었다...", 0.2f, EventEffectType.DamageHP, 30f, 0f, true),
                },
                new List<ItemInteraction> {
                    ItemInteract("PoisonAntidote", "해독제로 물을 정화하여 안전하게 마셨다.",
                        Outcome("완전히 회복되었다!", 1f, EventEffectType.FullRestore, 0f, 0f)),
                }) ? 1 : 0;

            count += CreateEvent("fountain_mana", "마력의 샘", "푸른빛이 도는 마법의 샘. MP를 회복해준다.",
                DungeonEventType.Fountain, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 10, "마력을 흡수한다",
                new List<EventOutcome> {
                    Outcome("마력이 충전되었다!", 0.85f, EventEffectType.HealMP, 100f, 0f),
                    Outcome("마력 과부하! 잠시 어지럽다...", 0.15f, EventEffectType.DamageHP, 20f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("fountain_blessed", "축복의 샘", "신성한 기운이 흐르는 샘. 상태이상을 정화한다.",
                DungeonEventType.Fountain, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 10, "몸을 담근다",
                new List<EventOutcome> {
                    Outcome("모든 저주가 풀렸다!", 1f, EventEffectType.RemoveStatus, 0f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("fountain_youth", "생명의 샘", "금빛 물이 빛나는 희귀한 샘. HP/MP 모두 회복.",
                DungeonEventType.Fountain, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.04f, 3, 10, "두 손으로 마신다",
                new List<EventOutcome> {
                    Outcome("생명력이 넘친다! HP/MP 전체 회복!", 1f, EventEffectType.FullRestore, 0f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("fountain_cursed", "저주받은 샘", "검은 물이 고인 불길한 샘. 위험하지만 보상이 크다.",
                DungeonEventType.Fountain, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 3, 10, "마신다",
                new List<EventOutcome> {
                    Outcome("저주의 대가로 강해졌다!", 0.5f, EventEffectType.CurseAndReward, 30f, 180f, true),
                    Outcome("저주에 저항했다! 경험치를 얻었다.", 0.3f, EventEffectType.GainExp, 200f, 0f),
                    Outcome("저주가 너무 강했다...", 0.2f, EventEffectType.DamageHP, 80f, 0f, true),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 큐리오 이벤트 15종 =====================
        private static int GenerateCurioEvents()
        {
            int count = 0;

            // 폐허 큐리오
            count += CreateEvent("curio_altar", "고대 제물대", "먼지 쌓인 제물대. 무언가를 바칠 수 있을 것 같다.",
                DungeonEventType.Curio, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 1, 10, "조사한다",
                new List<EventOutcome> {
                    Outcome("제물대에서 골드를 발견했다!", 0.4f, EventEffectType.GainGold, 150f, 0f),
                    Outcome("함정이었다! 독가스가 분출된다!", 0.3f, EventEffectType.ApplyStatus, 0f, 10f, true),
                    Outcome("아무것도 없었다.", 0.3f, EventEffectType.GainExp, 10f, 0f),
                },
                new List<ItemInteraction> {
                    ItemInteract("ResurrectionScroll", "부활 두루마리를 제물로 바쳤다. 큰 보상을 받았다!",
                        Outcome("신의 축복! 대량의 골드와 경험치!", 1f, EventEffectType.GainGold, 500f, 0f)),
                }) ? 1 : 0;

            count += CreateEvent("curio_bookshelf", "낡은 책장", "먼지투성이 책장. 고서가 꽂혀있다.",
                DungeonEventType.Curio, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 8, "책을 읽는다",
                new List<EventOutcome> {
                    Outcome("유용한 지식을 얻었다!", 0.5f, EventEffectType.GainExp, 100f, 0f),
                    Outcome("저주의 문구였다! 일시적 약화...", 0.2f, EventEffectType.DebuffStat, 3f, 60f, true),
                    Outcome("빈 페이지뿐이다.", 0.3f, EventEffectType.GainExp, 5f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_coffin", "봉인된 관", "묵직한 돌 관. 열어볼 용기가 있는가?",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 10, "관을 연다",
                new List<EventOutcome> {
                    Outcome("보물이 들어있었다!", 0.35f, EventEffectType.GainGold, 300f, 0f),
                    Outcome("언데드가 깨어났다!", 0.35f, EventEffectType.SpawnMonster, 2f, 0f, true),
                    Outcome("빈 관이었다. 먼지만 날린다.", 0.3f, EventEffectType.GainExp, 20f, 0f),
                },
                new List<ItemInteraction> {
                    ItemInteract("IdentifyScroll", "식별 주문으로 관의 내용물을 미리 확인했다.",
                        Outcome("안전하게 보물을 획득했다!", 1f, EventEffectType.GainGold, 400f, 0f)),
                }) ? 1 : 0;

            count += CreateEvent("curio_holy_fountain", "성수 웅덩이", "은빛 물이 고인 신성한 웅덩이.",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 2, 10, "성수를 뿌린다",
                new List<EventOutcome> {
                    Outcome("성수의 축복을 받았다!", 0.7f, EventEffectType.HealPercent, 30f, 0f),
                    Outcome("성수가 마르면서 폭발했다!", 0.3f, EventEffectType.DamageHP, 40f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("curio_mushroom", "빛나는 버섯 군락", "형형색색의 거대 버섯이 자라고 있다.",
                DungeonEventType.Curio, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 1, 7, "버섯을 먹는다",
                new List<EventOutcome> {
                    Outcome("힘이 솟는다! 일시적 강화!", 0.3f, EventEffectType.BuffStat, 5f, 90f),
                    Outcome("환각에 빠졌다... 잠시 혼란.", 0.25f, EventEffectType.DebuffStat, 3f, 30f, true),
                    Outcome("독버섯이었다!", 0.25f, EventEffectType.ApplyStatus, 0f, 8f, true),
                    Outcome("맛있다! HP가 회복되었다.", 0.2f, EventEffectType.HealHP, 50f, 0f),
                },
                new List<ItemInteraction> {
                    ItemInteract("PoisonAntidote", "해독제로 독성을 중화시킨 후 먹었다.",
                        Outcome("안전하게 버섯의 힘을 흡수했다!", 1f, EventEffectType.BuffStat, 7f, 120f)),
                }) ? 1 : 0;

            count += CreateEvent("curio_skeleton", "모험가의 유해", "이전 모험가의 뼈가 장비와 함께 놓여있다.",
                DungeonEventType.Curio, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.09f, 1, 10, "수색한다",
                new List<EventOutcome> {
                    Outcome("골드 주머니를 발견했다!", 0.4f, EventEffectType.GainGold, 100f, 0f),
                    Outcome("저주받은 장비였다!", 0.2f, EventEffectType.DebuffStat, 2f, 60f, true),
                    Outcome("일기장을 발견했다. 유용한 정보가 적혀있다.", 0.2f, EventEffectType.RevealMap, 0f, 0f),
                    Outcome("아무것도 쓸만한 게 없다.", 0.2f, EventEffectType.GainExp, 15f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_chest_mimic", "의심스러운 상자", "어딘가 이상한 보물상자. 열어볼까?",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 3, 10, "상자를 연다",
                new List<EventOutcome> {
                    Outcome("진짜 보물이었다!", 0.4f, EventEffectType.GainGold, 400f, 0f),
                    Outcome("미믹이었다! 기습 공격!", 0.4f, EventEffectType.DamageHP, 60f, 0f, true),
                    Outcome("빈 상자... 낚였다.", 0.2f, EventEffectType.GainExp, 30f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_crystal", "수정 군락", "빛나는 수정이 벽에서 자라고 있다.",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 3, 10, "수정을 캔다",
                new List<EventOutcome> {
                    Outcome("귀한 수정을 얻었다! 골드 획득!", 0.5f, EventEffectType.GainGold, 250f, 0f),
                    Outcome("수정이 폭발했다!", 0.3f, EventEffectType.DamageHP, 50f, 0f, true),
                    Outcome("가치 없는 돌멩이뿐...", 0.2f, EventEffectType.GainExp, 20f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_iron_maiden", "고문 장치", "녹슨 고문 장치. 안에 무언가 있는 것 같다.",
                DungeonEventType.Curio, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 4, 10, "열어본다",
                new List<EventOutcome> {
                    Outcome("고대 보물을 발견했다!", 0.3f, EventEffectType.GainGold, 600f, 0f),
                    Outcome("함정! 가시에 찔렸다!", 0.4f, EventEffectType.DamageHP, 80f, 0f, true),
                    Outcome("텅 비어있다.", 0.3f, EventEffectType.GainExp, 40f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_ritual_circle", "마법진", "바닥에 그려진 복잡한 마법진이 빛나고 있다.",
                DungeonEventType.Curio, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.04f, 4, 10, "마법진에 들어간다",
                new List<EventOutcome> {
                    Outcome("마력이 폭발적으로 증가했다!", 0.35f, EventEffectType.BuffStat, 10f, 90f),
                    Outcome("마법진이 폭주했다!", 0.35f, EventEffectType.DamageHP, 70f, 0f, true),
                    Outcome("마법진이 소멸했다. 경험치를 얻었다.", 0.3f, EventEffectType.GainExp, 150f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_statue", "신비한 석상", "고대 영웅의 석상. 경건한 기운이 느껴진다.",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 10, "경배한다",
                new List<EventOutcome> {
                    Outcome("석상이 빛나며 축복을 내렸다!", 0.4f, EventEffectType.RandomBuff, 0f, 120f),
                    Outcome("석상이 무너졌다... 아무 일도 없다.", 0.3f, EventEffectType.GainExp, 25f, 0f),
                    Outcome("석상의 눈에서 빛이! 골드 발견!", 0.3f, EventEffectType.GainGold, 200f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("curio_well", "소원의 우물", "동전을 던지면 소원을 들어준다는 우물.",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 1, 10, "동전을 던진다",
                new List<EventOutcome> {
                    Outcome("소원이 이루어졌다! 랜덤 버프!", 0.4f, EventEffectType.RandomBuff, 0f, 180f),
                    Outcome("우물이 골드를 토해냈다!", 0.25f, EventEffectType.GainGold, 200f, 0f),
                    Outcome("동전만 잃었다...", 0.35f, EventEffectType.LoseGold, 50f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("curio_treasure_goblin", "트레저 고블린 흔적", "반짝이는 금화 흔적이 보인다!",
                DungeonEventType.Curio, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 2, 10, "흔적을 따라간다",
                new List<EventOutcome> {
                    Outcome("트레저 고블린을 잡았다! 대량의 골드!", 0.3f, EventEffectType.GainGold, 800f, 0f),
                    Outcome("도망쳤다... 흘린 골드라도 줍자.", 0.4f, EventEffectType.GainGold, 150f, 0f),
                    Outcome("함정이었다! 매복 공격!", 0.3f, EventEffectType.SpawnMonster, 3f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("curio_dice", "운명의 주사위", "6면체 주사위가 빛나며 떠있다. 결과는 운에 달렸다.",
                DungeonEventType.Curio, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 1, 10, "주사위를 던진다",
                new List<EventOutcome> {
                    Outcome("6! 대박! 골드와 경험치를 대량 획득!", 0.167f, EventEffectType.GainGold, 1000f, 0f),
                    Outcome("5! 좋은 결과. 경험치 획득.", 0.167f, EventEffectType.GainExp, 300f, 0f),
                    Outcome("4! 랜덤 버프를 받았다.", 0.167f, EventEffectType.RandomBuff, 0f, 120f),
                    Outcome("3! 보통. HP가 회복되었다.", 0.167f, EventEffectType.HealPercent, 25f, 0f),
                    Outcome("2! 안 좋다... 골드를 잃었다.", 0.166f, EventEffectType.LoseGold, 100f, 0f, true),
                    Outcome("1! 최악! 피해를 입었다...", 0.166f, EventEffectType.DamageHP, 100f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("curio_mirror", "깨진 거울", "금이 간 마법 거울. 미래를 보여준다고 한다.",
                DungeonEventType.Curio, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 3, 10, "거울을 들여다본다",
                new List<EventOutcome> {
                    Outcome("미래를 보았다! 지도가 공개된다!", 0.4f, EventEffectType.RevealMap, 0f, 0f),
                    Outcome("거울 속 자신에게 공격당했다!", 0.3f, EventEffectType.DamageHP, 50f, 0f, true),
                    Outcome("거울이 산산조각났다. 유리 조각에서 경험치를 흡수.", 0.3f, EventEffectType.GainExp, 200f, 0f),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 보물방 이벤트 5종 =====================
        private static int GenerateTreasureEvents()
        {
            int count = 0;

            count += CreateEvent("treasure_common", "낡은 보물 상자", "먼지 쌓인 보물 상자. 기본적인 보상이 들어있다.",
                DungeonEventType.TreasureRoom, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.10f, 1, 10, "상자를 연다",
                new List<EventOutcome> {
                    Outcome("골드를 발견했다!", 1f, EventEffectType.GainGold, 100f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("treasure_silver", "은빛 보물 상자", "정교하게 만들어진 은빛 상자.",
                DungeonEventType.TreasureRoom, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 10, "상자를 연다",
                new List<EventOutcome> {
                    Outcome("좋은 보물을 발견했다!", 1f, EventEffectType.GainGold, 300f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("treasure_gold", "금빛 보물 상자", "눈부시게 빛나는 금빛 상자!",
                DungeonEventType.TreasureRoom, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 4, 10, "상자를 연다",
                new List<EventOutcome> {
                    Outcome("엄청난 보물이다!", 1f, EventEffectType.GainGold, 700f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("treasure_cursed", "저주받은 상자", "검은 기운이 흐르는 상자. 보상은 크지만...",
                DungeonEventType.TreasureRoom, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 3, 10, "상자를 연다",
                new List<EventOutcome> {
                    Outcome("저주와 함께 큰 보상!", 0.6f, EventEffectType.GainGold, 500f, 0f),
                    Outcome("저주만 걸렸다...", 0.4f, EventEffectType.DebuffStat, 5f, 120f, true),
                }) ? 1 : 0;

            count += CreateEvent("treasure_legendary", "전설의 보물함", "고대 문명의 보물함. 전설적인 보상이 기다린다!",
                DungeonEventType.TreasureRoom, DungeonEventRarity.Legendary,
                DungeonEventTrigger.ChanceBased, 0.01f, 7, 10, "봉인을 해제한다",
                new List<EventOutcome> {
                    Outcome("전설의 보물! 엄청난 골드와 경험치!", 1f, EventEffectType.GainGold, 2000f, 0f),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 매복/함정 이벤트 8종 =====================
        private static int GenerateAmbushEvents()
        {
            int count = 0;

            count += CreateEvent("ambush_goblin", "고블린 매복", "고블린들이 숨어있다! 기습 공격!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 5, "",
                new List<EventOutcome> {
                    Outcome("고블린 매복! 조심해!", 1f, EventEffectType.SpawnMonster, 3f, 0f, true),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Goblin", "Normal", 3, 0f, false),
                }) ? 1 : 0;

            count += CreateEvent("ambush_undead", "언데드 습격", "땅에서 해골이 솟아오른다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 3, 8, "",
                new List<EventOutcome> {
                    Outcome("언데드가 부활한다!", 1f, EventEffectType.SpawnMonster, 4f, 0f, true),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Undead", "Normal", 4, 0f, false),
                    Wave(2, "Undead", "Elite", 1, 3f, true),
                }) ? 1 : 0;

            count += CreateEvent("ambush_spider", "거미줄 함정", "거미줄에 걸렸다! 거미들이 몰려온다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 2, 7, "",
                new List<EventOutcome> {
                    Outcome("거미줄에 걸렸다! 이동속도 감소!", 1f, EventEffectType.DebuffStat, 3f, 15f, true),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Beast", "Normal", 5, 0f, false),
                }) ? 1 : 0;

            count += CreateEvent("ambush_demon", "악마의 소환진", "바닥의 마법진이 갑자기 빛나기 시작한다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.04f, 5, 10, "",
                new List<EventOutcome> {
                    Outcome("악마가 소환된다!", 1f, EventEffectType.SpawnMonster, 2f, 0f, true),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Demon", "Normal", 2, 0f, false),
                    Wave(2, "Demon", "Elite", 1, 5f, true),
                }) ? 1 : 0;

            count += CreateEvent("trap_spike", "가시 함정", "바닥에서 가시가 솟아오른다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.08f, 1, 10, "",
                new List<EventOutcome> {
                    Outcome("가시에 찔렸다!", 1f, EventEffectType.DamageHP, 40f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("trap_poison_gas", "독가스 함정", "보라색 가스가 분출된다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.07f, 2, 10, "",
                new List<EventOutcome> {
                    Outcome("독가스에 중독되었다!", 1f, EventEffectType.ApplyStatus, 0f, 10f, true),
                }) ? 1 : 0;

            count += CreateEvent("trap_fire", "화염 함정", "불꽃이 벽에서 분출된다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.06f, 3, 10, "",
                new List<EventOutcome> {
                    Outcome("화염에 화상을 입었다!", 1f, EventEffectType.DamageHP, 60f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("trap_teleport", "전이 함정", "마법진에 밟자 순간이동 되었다!",
                DungeonEventType.AmbushTrap, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 2, 10, "",
                new List<EventOutcome> {
                    Outcome("알 수 없는 곳으로 전이되었다!", 1f, EventEffectType.Teleport, 0f, 0f, true),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 아레나 이벤트 5종 =====================
        private static int GenerateArenaEvents()
        {
            int count = 0;

            count += CreateEvent("arena_basic", "시련의 방", "문이 잠기고 몬스터가 소환된다! 모두 처치해야 한다.",
                DungeonEventType.Arena, DungeonEventRarity.Common,
                DungeonEventTrigger.ChanceBased, 0.06f, 1, 10, "",
                new List<EventOutcome> {
                    Outcome("시련을 극복했다! 보상 획득!", 1f, EventEffectType.GainGold, 200f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Goblin", "Normal", 5, 0f, false),
                    Wave(2, "Goblin", "Elite", 2, 3f, true),
                }) ? 1 : 0;

            count += CreateEvent("arena_elite", "정예의 방", "강력한 엘리트 몬스터가 기다린다!",
                DungeonEventType.Arena, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 3, 10, "",
                new List<EventOutcome> {
                    Outcome("정예를 처치했다! 고급 보상!", 1f, EventEffectType.GainGold, 500f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Orc", "Elite", 2, 0f, true),
                    Wave(2, "Orc", "Berserker", 1, 5f, false),
                }) ? 1 : 0;

            count += CreateEvent("arena_survival", "생존의 방", "45초간 밀려오는 적을 버텨라!",
                DungeonEventType.Arena, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 2, 10, "",
                new List<EventOutcome> {
                    Outcome("생존 성공! 보상 획득!", 1f, EventEffectType.GainGold, 350f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Beast", "Normal", 6, 0f, false),
                    Wave(2, "Beast", "Normal", 6, 10f, false),
                    Wave(3, "Beast", "Elite", 3, 20f, true),
                }) ? 1 : 0;

            count += CreateEvent("arena_boss_rush", "보스의 방", "미니 보스와의 결전!",
                DungeonEventType.Arena, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.02f, 5, 10, "",
                new List<EventOutcome> {
                    Outcome("보스를 처치했다! 특급 보상!", 1f, EventEffectType.GainGold, 1000f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Demon", "Boss", 1, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("arena_gauntlet", "무한의 전장", "끝없는 적의 파도! 버틸 수 있는 만큼 버텨라!",
                DungeonEventType.Arena, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 4, 10, "",
                new List<EventOutcome> {
                    Outcome("웨이브당 보상 증가!", 1f, EventEffectType.GainGold, 100f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Goblin", "Normal", 5, 0f, false),
                    Wave(2, "Orc", "Normal", 4, 5f, false),
                    Wave(3, "Undead", "Elite", 3, 10f, true),
                    Wave(4, "Demon", "Berserker", 2, 15f, false),
                    Wave(5, "Dragon", "Normal", 1, 20f, true),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 시련 이벤트 5종 =====================
        private static int GenerateTrialEvents()
        {
            int count = 0;

            count += CreateEvent("trial_no_damage", "무상의 시련", "피해를 받지 않고 모든 적을 처치하라!",
                DungeonEventType.Trial, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 3, 10, "도전한다",
                new List<EventOutcome> {
                    Outcome("완벽한 전투! 특별 보상!", 1f, EventEffectType.GainGold, 800f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Elemental", "Normal", 4, 0f, false),
                }) ? 1 : 0;

            count += CreateEvent("trial_speed", "속도의 시련", "제한 시간 안에 모든 적을 처치하라! (30초)",
                DungeonEventType.Trial, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 2, 10, "도전한다",
                new List<EventOutcome> {
                    Outcome("시간 내 처치! 보상 획득!", 1f, EventEffectType.GainExp, 500f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Goblin", "Normal", 8, 0f, false),
                },
                combatTimeLimit: 30f) ? 1 : 0;

            count += CreateEvent("trial_isolation", "고립의 시련", "홀로 강적과 맞서라!",
                DungeonEventType.Trial, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 4, 10, "도전한다",
                new List<EventOutcome> {
                    Outcome("강적을 쓰러뜨렸다!", 1f, EventEffectType.GainGold, 600f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Construct", "Boss", 1, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("trial_endurance", "인내의 시련", "5웨이브를 돌파하라! 회복 불가!",
                DungeonEventType.Trial, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 5, 10, "도전한다",
                new List<EventOutcome> {
                    Outcome("인내의 승리! 대량 보상!", 1f, EventEffectType.GainGold, 1200f, 0f),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Orc", "Normal", 3, 0f, false),
                    Wave(2, "Undead", "Normal", 4, 5f, false),
                    Wave(3, "Beast", "Elite", 2, 10f, true),
                    Wave(4, "Elemental", "Berserker", 2, 15f, false),
                    Wave(5, "Demon", "Leader", 1, 20f, true),
                }) ? 1 : 0;

            count += CreateEvent("trial_sacrifice", "희생의 시련", "HP 50%를 바치면 엄청난 보상!",
                DungeonEventType.Trial, DungeonEventRarity.Epic,
                DungeonEventTrigger.ChanceBased, 0.02f, 5, 10, "피를 바친다",
                new List<EventOutcome> {
                    Outcome("희생이 인정되었다! 신의 보상!", 0.7f, EventEffectType.GainGold, 2000f, 0f),
                    Outcome("희생이 거부되었다...", 0.3f, EventEffectType.DamageHP, 50f, 0f, true),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 상인 이벤트 3종 =====================
        private static int GenerateShopEvents()
        {
            int count = 0;

            count += CreateEvent("shop_wandering", "떠돌이 상인", "던전 속 떠돌이 상인. 희귀한 물건을 판다.",
                DungeonEventType.Shop, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.05f, 2, 10, "상점 열기",
                shopItems: new List<EventShopItem> {
                    ShopItemData("HealthPotion_Large", 80, 3, 0.2f, 20f),
                    ShopItemData("ManaPotion_Large", 80, 3, 0.2f, 20f),
                    ShopItemData("StrengthScroll", 40, 2, 0.1f, 10f),
                    ShopItemData("SpeedScroll", 40, 2, 0.1f, 10f),
                    ShopItemData("ProtectionScroll", 40, 2, 0.1f, 10f),
                }) ? 1 : 0;

            count += CreateEvent("shop_rare", "비밀 상인", "어둠 속에서 나타난 비밀 상인. 진귀한 물건이 있다.",
                DungeonEventType.Shop, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 4, 10, "상점 열기",
                shopItems: new List<EventShopItem> {
                    ShopItemData("ResurrectionScroll", 400, 1, 0.05f, 10f),
                    ShopItemData("HealthPotion_Max", 250, 2, 0.1f, 15f),
                    ShopItemData("ManaPotion_Max", 250, 2, 0.1f, 15f),
                    ShopItemData("TownPortal", 150, 1, 0f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("shop_gamble", "도박꾼", "수상한 도박꾼. 100골드에 미지의 아이템을 뽑을 수 있다.",
                DungeonEventType.Gamble, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 2, 10, "도박한다",
                new List<EventOutcome> {
                    Outcome("대박! 고급 아이템 획득!", 0.1f, EventEffectType.GainGold, 500f, 0f),
                    Outcome("꽤 좋은 물건이다!", 0.25f, EventEffectType.GainGold, 200f, 0f),
                    Outcome("그저 그런 물건...", 0.35f, EventEffectType.GainGold, 50f, 0f),
                    Outcome("쓰레기를 뽑았다...", 0.3f, EventEffectType.LoseGold, 100f, 0f, true),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 포탈 이벤트 4종 =====================
        private static int GeneratePortalEvents()
        {
            int count = 0;

            count += CreateEvent("portal_treasure", "보물 포탈", "빛나는 포탈이 비밀 보물방으로 이어진다!",
                DungeonEventType.Portal, DungeonEventRarity.Rare,
                DungeonEventTrigger.ChanceBased, 0.03f, 3, 10, "포탈에 들어간다",
                new List<EventOutcome> {
                    Outcome("비밀 보물방에 도착! 보물이 가득!", 0.7f, EventEffectType.GainGold, 600f, 0f),
                    Outcome("함정이었다! 위험한 방에 전이되었다!", 0.3f, EventEffectType.SpawnMonster, 5f, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("portal_chaos", "혼돈의 문", "불안정한 포탈. 어디로 갈지 알 수 없다.",
                DungeonEventType.Portal, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 2, 10, "포탈에 들어간다",
                new List<EventOutcome> {
                    Outcome("다음 층으로 순간이동!", 0.3f, EventEffectType.Teleport, 1f, 0f),
                    Outcome("랜덤 버프를 받았다!", 0.3f, EventEffectType.RandomBuff, 0f, 120f),
                    Outcome("무작위 피해를 입었다!", 0.2f, EventEffectType.DamageHP, 40f, 0f, true),
                    Outcome("같은 층의 다른 위치로 이동.", 0.2f, EventEffectType.Teleport, 0f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("portal_boss", "도전의 포탈", "강력한 보스가 기다리는 차원의 틈!",
                DungeonEventType.Portal, DungeonEventRarity.Epic,
                DungeonEventTrigger.ChanceBased, 0.01f, 6, 10, "포탈에 들어간다",
                new List<EventOutcome> {
                    Outcome("차원의 보스에 도전!", 1f, EventEffectType.SpawnMonster, 1f, 0f, true),
                },
                combatWaves: new List<CombatWave> {
                    Wave(1, "Dragon", "Boss", 1, 0f, true),
                }) ? 1 : 0;

            count += CreateEvent("portal_escape", "귀환 포탈", "마을로 돌아갈 수 있는 포탈. 한 번 사용하면 사라진다.",
                DungeonEventType.Portal, DungeonEventRarity.Rare,
                DungeonEventTrigger.ConditionBased, 0.05f, 5, 10, "포탈에 들어간다",
                new List<EventOutcome> {
                    Outcome("마을로 안전하게 귀환!", 1f, EventEffectType.Teleport, 0f, 0f),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 휴식지 이벤트 3종 =====================
        private static int GenerateRestEvents()
        {
            int count = 0;

            count += CreateEvent("rest_campfire", "모닥불", "따뜻한 모닥불. 잠시 쉬어갈 수 있다.",
                DungeonEventType.RestSite, DungeonEventRarity.Common,
                DungeonEventTrigger.FloorGuaranteed, 1f, 5, 10, "쉬어간다",
                new List<EventOutcome> {
                    Outcome("따뜻하다... HP 30% 회복.", 1f, EventEffectType.HealPercent, 30f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("rest_sanctuary", "성역", "안전한 기도실. 완전한 회복이 가능하다.",
                DungeonEventType.RestSite, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.03f, 5, 10, "기도한다",
                new List<EventOutcome> {
                    Outcome("성역의 힘으로 완전히 회복!", 1f, EventEffectType.FullRestore, 0f, 0f),
                }) ? 1 : 0;

            count += CreateEvent("rest_meditation", "명상의 방", "고요한 방. 명상하면 버프를 받을 수 있다.",
                DungeonEventType.RestSite, DungeonEventRarity.Uncommon,
                DungeonEventTrigger.ChanceBased, 0.04f, 3, 10, "명상한다",
                new List<EventOutcome> {
                    Outcome("마음이 고요해졌다. HP/MP 50% 회복 + 랜덤 버프!", 1f, EventEffectType.HealPercent, 50f, 0f),
                }) ? 1 : 0;

            return count;
        }

        // ===================== 유틸리티 메서드 =====================

        private static bool CreateEvent(string id, string name, string desc,
            DungeonEventType type, DungeonEventRarity rarity,
            DungeonEventTrigger trigger, float spawnChance, int minFloor, int maxFloor,
            string interactionText,
            List<EventOutcome> outcomes = null,
            List<ItemInteraction> itemInteractions = null,
            List<CombatWave> combatWaves = null,
            List<EventShopItem> shopItems = null,
            float combatTimeLimit = 0f)
        {
            string assetPath = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<DungeonEventData>(assetPath) != null) return false;

            var data = ScriptableObject.CreateInstance<DungeonEventData>();
            var so = new SerializedObject(data);

            so.FindProperty("eventId").stringValue = id;
            so.FindProperty("eventName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("eventType").enumValueIndex = (int)type;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("triggerType").enumValueIndex = (int)trigger;
            so.FindProperty("spawnChance").floatValue = spawnChance;
            so.FindProperty("minFloor").intValue = minFloor;
            so.FindProperty("maxFloor").intValue = maxFloor;
            so.FindProperty("interactionText").stringValue = interactionText ?? "";
            so.FindProperty("combatTimeLimit").floatValue = combatTimeLimit;

            // Glow colors by type
            var glowProp = so.FindProperty("glowColor");
            Color glow = type switch
            {
                DungeonEventType.Shrine => new Color(1f, 0.84f, 0f),
                DungeonEventType.Fountain => new Color(0.3f, 0.6f, 1f),
                DungeonEventType.Curio => new Color(0.8f, 0.8f, 0.8f),
                DungeonEventType.TreasureRoom => new Color(1f, 0.85f, 0.3f),
                DungeonEventType.AmbushTrap => new Color(1f, 0.2f, 0.2f),
                DungeonEventType.Arena => new Color(1f, 0.5f, 0f),
                DungeonEventType.Trial => new Color(0.8f, 0.4f, 1f),
                DungeonEventType.Shop => new Color(0.4f, 1f, 0.4f),
                DungeonEventType.Portal => new Color(0.5f, 0f, 1f),
                DungeonEventType.RestSite => new Color(0.3f, 0.8f, 0.3f),
                DungeonEventType.Gamble => new Color(1f, 1f, 0f),
                _ => Color.white
            };
            glowProp.colorValue = glow;

            so.ApplyModifiedProperties();

            // Outcomes (must set via reflection since SerializedProperty for complex lists is cumbersome)
            if (outcomes != null)
            {
                var field = typeof(DungeonEventData).GetField("bareHandOutcomes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(data, outcomes);
            }

            if (itemInteractions != null)
            {
                var field = typeof(DungeonEventData).GetField("itemInteractions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(data, itemInteractions);
            }

            if (combatWaves != null)
            {
                var field = typeof(DungeonEventData).GetField("combatWaves",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(data, combatWaves);
            }

            if (shopItems != null)
            {
                var field = typeof(DungeonEventData).GetField("shopItems",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(data, shopItems);
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.CreateAsset(data, assetPath);
            return true;
        }

        // Helper: EventOutcome 생성
        private static EventOutcome Outcome(string desc, float chance, EventEffectType effect,
            float value, float duration, bool negative = false)
        {
            return new EventOutcome
            {
                description = desc,
                chance = chance,
                effectType = effect,
                effectValue = value,
                duration = duration,
                isNegative = negative
            };
        }

        // Helper: ItemInteraction 생성
        private static ItemInteraction ItemInteract(string itemName, string result, EventOutcome outcome)
        {
            return new ItemInteraction
            {
                requiredItemName = itemName,
                resultDescription = result,
                guaranteedOutcome = outcome
            };
        }

        // Helper: CombatWave 생성
        private static CombatWave Wave(int num, string race, string variant, int count, float delay, bool elite)
        {
            return new CombatWave
            {
                waveNumber = num,
                monsterRace = race,
                monsterVariant = variant,
                count = count,
                delayBeforeWave = delay,
                isElite = elite
            };
        }

        // Helper: EventShopItem 생성
        private static EventShopItem ShopItemData(string name, int price, int stock, float discountChance, float discountPercent)
        {
            return new EventShopItem
            {
                itemName = name,
                price = price,
                stock = stock,
                discountChance = discountChance,
                discountPercent = discountPercent
            };
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
