using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// 월드 랜덤 이벤트 데이터 35종 자동 생성
    /// 트레저헌터5 + 랜덤습격8 + 특수NPC5 + 환경이벤트7 + 보물발견5 + 미니퀘스트5
    /// </summary>
    public class WorldEventDataGenerator
    {
        private static string basePath = "Assets/Resources/ScriptableObjects/WorldEvents";

        [MenuItem("Dungeon Crawler/Generate World Event Data")]
        public static void Generate()
        {
            EnsureFolder(basePath);

            int total = 0;
            total += GenerateTreasureHunterEvents();   // 5
            total += GenerateRandomRaidEvents();        // 8
            total += GenerateSpecialNPCEvents();        // 5
            total += GenerateEnvironmentEvents();       // 7
            total += GenerateTreasureDiscoveryEvents(); // 5
            total += GenerateMiniQuestEvents();         // 5

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WorldEventDataGenerator] {total}개 월드 이벤트 생성 완료");
        }

        // ===================== 트레저 헌터 5종 =====================
        private static int GenerateTreasureHunterEvents()
        {
            int count = 0;

            // 1. 트레저 고블린 - 일반
            count += CreateEvent("we_treasure_goblin", "트레저 고블린", "금화 주머니를 든 고블린이 도망치고 있다! 잡으면 대량의 골드를 획득!",
                WorldEventType.TreasureGoblin, WorldEventRarity.Common,
                0.15f, 30f, 1, 99, false, false, WeatherCondition.Any,
                45f, false, 8f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_normal", 1, 100, 110, false, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 500, 1f),
                    Reward(WorldRewardType.Experience, 200, 1f)
                }) ? 1 : 0;

            // 2. 황금 고블린 - 희귀
            count += CreateEvent("we_golden_goblin", "황금 고블린", "온 몸이 금빛으로 빛나는 특별한 고블린! 엄청난 보물을 갖고 있다!",
                WorldEventType.TreasureGoblin, WorldEventRarity.Rare,
                0.05f, 60f, 3, 99, false, false, WeatherCondition.Any,
                30f, false, 10f, true, "황금 고블린이 출현했습니다!",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_elite", 1, 110, 120, false, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 2000, 1f),
                    Reward(WorldRewardType.Experience, 500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "Longsword_Rare")
                }) ? 1 : 0;

            // 3. 보물 운반대 - 비일반
            count += CreateEvent("we_treasure_convoy", "보물 운반대", "고블린들이 보물 수레를 운반하고 있다! 습격하여 보물을 탈취하라!",
                WorldEventType.TreasureGoblin, WorldEventRarity.Uncommon,
                0.08f, 45f, 2, 99, false, false, WeatherCondition.Any,
                60f, false, 12f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_normal", 3, 100, 105, false, 0),
                    Monster("goblin_berserker", 1, 105, 110, false, 2f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1000, 1f),
                    Reward(WorldRewardType.Experience, 300, 1f)
                }) ? 1 : 0;

            // 4. 드래곤 보물고 - 에픽
            count += CreateEvent("we_dragon_hoard", "드래곤 보물고", "드래곤이 잠시 자리를 비운 보물고! 빨리 보물을 가져가자!",
                WorldEventType.TreasureGoblin, WorldEventRarity.Epic,
                0.02f, 120f, 8, 99, false, false, WeatherCondition.Any,
                20f, false, 15f, true, "드래곤의 보물고가 발견되었습니다!",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 5000, 1f),
                    Reward(WorldRewardType.Experience, 1000, 1f),
                    Reward(WorldRewardType.Item, 1, 0.5f, "Greatsword_Epic")
                },
                choices: new WorldEventChoice[] {
                    Choice("보물을 조심스럽게 가져간다", "안전하게 일부만 가져왔다.", WorldRewardType.Gold, 2000, 0f, 0),
                    Choice("전부 쓸어담는다", "드래곤이 돌아오기 전에 전부 가져왔다... 하지만 위험!", WorldRewardType.Gold, 5000, 0.4f, 200)
                }) ? 1 : 0;

            // 5. 전설의 보물 사냥꾼 - 전설
            count += CreateEvent("we_legendary_treasure_hunter", "전설의 보물 사냥꾼", "고대의 보물 사냥꾼 유령이 나타났다! 시련을 통과하면 전설적인 보물을 준다!",
                WorldEventType.TreasureGoblin, WorldEventRarity.Legendary,
                0.01f, 180f, 10, 99, true, false, WeatherCondition.Any,
                120f, true, 20f, true, "전설의 보물 사냥꾼이 출현했습니다!",
                monsters: new WorldEventMonster[] {
                    Monster("demon_boss", 1, 115, 120, true, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 10000, 1f),
                    Reward(WorldRewardType.Experience, 3000, 1f),
                    Reward(WorldRewardType.SkillPoint, 1, 1f)
                },
                dialogues: new WorldEventDialogue[] {
                    Dialogue("보물 사냥꾼", "나의 시련을 통과하라... 그러면 전설의 보물을 주겠다.", 0),
                    Dialogue("보물 사냥꾼", "네 실력이 인정받았다. 이것을 받아라.", 1)
                }) ? 1 : 0;

            return count;
        }

        // ===================== 랜덤 습격 8종 =====================
        private static int GenerateRandomRaidEvents()
        {
            int count = 0;

            // 1. 고블린 습격 - 일반
            count += CreateEvent("we_goblin_raid", "고블린 습격", "고블린 무리가 몰려온다! 물리치고 전리품을 챙기자!",
                WorldEventType.MonsterRaid, WorldEventRarity.Common,
                0.12f, 30f, 1, 99, false, false, WeatherCondition.Any,
                90f, false, 15f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_normal", 5, 100, 105, false, 0),
                    Monster("goblin_berserker", 2, 100, 108, false, 3f),
                    Monster("goblin_leader", 1, 105, 112, false, 6f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 300, 1f),
                    Reward(WorldRewardType.Experience, 250, 1f)
                }) ? 1 : 0;

            // 2. 오크 전쟁 부대
            count += CreateEvent("we_orc_warband", "오크 전쟁 부대", "오크 전투 부대가 접근 중! 강력한 전사들을 주의하라!",
                WorldEventType.MonsterRaid, WorldEventRarity.Uncommon,
                0.08f, 45f, 3, 99, false, false, WeatherCondition.Any,
                120f, false, 12f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("orc_normal", 4, 102, 108, false, 0),
                    Monster("orc_berserker", 2, 105, 112, false, 4f),
                    Monster("orc_leader", 1, 108, 115, false, 8f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 600, 1f),
                    Reward(WorldRewardType.Experience, 400, 1f)
                }) ? 1 : 0;

            // 3. 야간 언데드 습격
            count += CreateEvent("we_undead_night_raid", "야간 언데드 습격", "밤의 어둠 속에서 언데드가 일어났다! 날이 밝기 전에 물리쳐라!",
                WorldEventType.NightHaunt, WorldEventRarity.Common,
                0.18f, 30f, 2, 99, true, false, WeatherCondition.Any,
                120f, false, 15f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("undead_normal", 6, 100, 106, false, 0),
                    Monster("undead_shaman", 2, 103, 110, false, 5f),
                    Monster("undead_elite", 1, 108, 114, false, 10f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 400, 1f),
                    Reward(WorldRewardType.Experience, 350, 1f)
                }) ? 1 : 0;

            // 4. 야수 떼 출현
            count += CreateEvent("we_beast_swarm", "야수 떼 출현", "거대한 야수 무리가 영역을 침범했다! 사냥하여 쓰러뜨려라!",
                WorldEventType.MonsterRaid, WorldEventRarity.Common,
                0.10f, 35f, 2, 99, false, false, WeatherCondition.Any,
                90f, false, 18f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("beast_normal", 8, 98, 105, false, 0),
                    Monster("beast_berserker", 2, 103, 110, false, 4f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 250, 1f),
                    Reward(WorldRewardType.Experience, 300, 1f)
                }) ? 1 : 0;

            // 5. 원소 폭풍 - 폭풍 날씨
            count += CreateEvent("we_elemental_storm", "원소 폭풍", "폭풍이 불어 원소 생명체들이 소환되었다! 번개 원소에 주의하라!",
                WorldEventType.ElementalRift, WorldEventRarity.Uncommon,
                0.15f, 40f, 4, 99, false, false, WeatherCondition.Storm,
                90f, false, 15f, true, "원소 폭풍이 몰려옵니다!",
                monsters: new WorldEventMonster[] {
                    Monster("elemental_normal", 4, 105, 112, false, 0),
                    Monster("elemental_elite", 2, 108, 115, false, 5f),
                    Monster("elemental_boss", 1, 112, 118, true, 10f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 800, 1f),
                    Reward(WorldRewardType.Experience, 600, 1f),
                    Reward(WorldRewardType.Item, 1, 0.2f, "CrystalStaff_Rare")
                }) ? 1 : 0;

            // 6. 악마 차원문
            count += CreateEvent("we_demon_portal", "악마 차원문", "어둠의 차원문이 열렸다! 악마들이 밀려나온다!",
                WorldEventType.ElementalRift, WorldEventRarity.Rare,
                0.04f, 60f, 6, 99, false, false, WeatherCondition.Any,
                120f, false, 12f, true, "악마의 차원문이 열렸습니다!",
                monsters: new WorldEventMonster[] {
                    Monster("demon_normal", 5, 106, 112, false, 0),
                    Monster("demon_shaman", 2, 108, 114, false, 4f),
                    Monster("demon_berserker", 2, 110, 116, false, 8f),
                    Monster("demon_boss", 1, 114, 120, true, 12f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1500, 1f),
                    Reward(WorldRewardType.Experience, 1000, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "DarkBlade_Epic")
                }) ? 1 : 0;

            // 7. 드래곤 습격 - 에픽
            count += CreateEvent("we_dragon_attack", "드래곤 습격", "거대한 드래곤이 하늘에서 내려왔다! 전 서버 최강 이벤트!",
                WorldEventType.DragonSighting, WorldEventRarity.Epic,
                0.02f, 120f, 8, 99, false, false, WeatherCondition.Any,
                180f, true, 25f, true, "드래곤이 출현했습니다! 모든 용사여 모여라!",
                monsters: new WorldEventMonster[] {
                    Monster("dragon_boss", 1, 118, 125, true, 0),
                    Monster("dragon_normal", 3, 110, 116, false, 10f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 5000, 1f),
                    Reward(WorldRewardType.Experience, 3000, 1f),
                    Reward(WorldRewardType.Item, 1, 0.5f, "Greatsword_Legendary"),
                    Reward(WorldRewardType.SkillPoint, 1, 0.2f)
                }) ? 1 : 0;

            // 8. 골렘 침공
            count += CreateEvent("we_golem_invasion", "골렘 침공", "고대 유적에서 골렘들이 깨어났다! 단단한 방어를 뚫어라!",
                WorldEventType.MonsterRaid, WorldEventRarity.Uncommon,
                0.06f, 50f, 5, 99, false, false, WeatherCondition.Any,
                120f, false, 14f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("construct_normal", 3, 104, 110, false, 0),
                    Monster("construct_elite", 2, 108, 114, false, 5f),
                    Monster("construct_leader", 1, 112, 118, false, 10f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 700, 1f),
                    Reward(WorldRewardType.Experience, 500, 1f)
                }) ? 1 : 0;

            return count;
        }

        // ===================== 특수 NPC 5종 =====================
        private static int GenerateSpecialNPCEvents()
        {
            int count = 0;

            // 1. 방랑 상인
            count += CreateEvent("we_wandering_merchant", "방랑 상인", "특별한 물건을 파는 방랑 상인이 나타났다! 놓치면 아쉬운 기회!",
                WorldEventType.WanderingMerchant, WorldEventRarity.Uncommon,
                0.10f, 40f, 1, 99, false, false, WeatherCondition.Any,
                120f, false, 5f, false, "",
                dialogues: new WorldEventDialogue[] {
                    Dialogue("방랑 상인", "어이, 여행자! 특별한 물건이 있는데 관심 있나?", 0),
                    Dialogue("방랑 상인", "이건 다른 데서는 절대 못 구하는 거야. 싸게 줄게.", 1)
                },
                choices: new WorldEventChoice[] {
                    Choice("물건을 구매한다 (500G)", "특별한 장비를 구매했다!", WorldRewardType.Item, 1, 0f, 0),
                    Choice("정보를 구매한다 (200G)", "유용한 정보를 얻었다!", WorldRewardType.Experience, 300, 0f, 0),
                    Choice("거절한다", "상인이 아쉬워하며 떠났다.", WorldRewardType.Gold, 0, 0f, 0)
                }) ? 1 : 0;

            // 2. 수수께끼 현자
            count += CreateEvent("we_mystery_sage", "수수께끼 현자", "수수께끼를 내는 신비로운 현자가 나타났다. 맞추면 보상, 틀리면 저주!",
                WorldEventType.MysteriousNPC, WorldEventRarity.Uncommon,
                0.08f, 45f, 3, 99, false, false, WeatherCondition.Any,
                90f, true, 5f, false, "",
                dialogues: new WorldEventDialogue[] {
                    Dialogue("수수께끼 현자", "지혜로운 자여, 나의 수수께끼에 답해보거라.", 0),
                    Dialogue("수수께끼 현자", "무엇이 가벼울수록 더 무거워지는가?", 1)
                },
                choices: new WorldEventChoice[] {
                    Choice("그림자", "정답이다! 지혜의 보상을 받아라.", WorldRewardType.Experience, 500, 0f, 0),
                    Choice("돌", "아쉽구나... 벌을 받아라.", WorldRewardType.Gold, 0, 0.8f, 50),
                    Choice("바람", "틀렸다. 하지만 용기에 대한 보상을 주지.", WorldRewardType.Gold, 100, 0.3f, 30)
                }) ? 1 : 0;

            // 3. 상인 캐러밴
            count += CreateEvent("we_merchant_caravan", "상인 캐러밴 호위", "상인 캐러밴이 호위를 요청한다. 무사히 목적지까지 호위하면 큰 보상!",
                WorldEventType.MerchantCaravan, WorldEventRarity.Uncommon,
                0.07f, 50f, 2, 99, false, true, WeatherCondition.Any,
                180f, true, 20f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_normal", 3, 100, 106, false, 5f),
                    Monster("orc_normal", 2, 102, 108, false, 30f),
                    Monster("beast_normal", 4, 100, 105, false, 60f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1000, 1f),
                    Reward(WorldRewardType.Experience, 500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.4f, "ChestArmor_Uncommon")
                },
                dialogues: new WorldEventDialogue[] {
                    Dialogue("상인 대장", "용사님! 산적들이 자주 출몰하는 길이라 호위가 필요합니다.", 0),
                    Dialogue("상인 대장", "무사히 도착하면 크게 보답하겠습니다!", 1),
                    Dialogue("상인 대장", "감사합니다! 약속대로 보상을 드리겠습니다.", 2)
                }) ? 1 : 0;

            // 4. 유령 조우
            count += CreateEvent("we_ghost_encounter", "유령 조우", "옛 전사의 유령이 나타났다. 원한을 풀어주면 보상을 준다.",
                WorldEventType.GhostEncounter, WorldEventRarity.Rare,
                0.04f, 60f, 5, 99, true, false, WeatherCondition.Any,
                120f, true, 8f, false, "",
                dialogues: new WorldEventDialogue[] {
                    Dialogue("전사의 유령", "나는 이곳에서 쓰러진 전사... 원한이 풀리지 않아 떠나지 못하고 있다.", 0),
                    Dialogue("전사의 유령", "내 원수를 갚아주겠는가, 젊은 전사여?", 1),
                    Dialogue("전사의 유령", "고맙다... 이제 편히 쉴 수 있겠구나. 이것은 내 유물이니 가져가거라.", 2)
                },
                choices: new WorldEventChoice[] {
                    Choice("원수를 갚아주겠다", "유령의 원수인 언데드를 처치했다!", WorldRewardType.Experience, 800, 0.1f, 80),
                    Choice("진혼의 기도를 올린다", "유령이 평화롭게 떠났다.", WorldRewardType.Experience, 400, 0f, 0),
                    Choice("유령을 공격한다", "유령이 분노했다!", WorldRewardType.Gold, 0, 0.9f, 150)
                }) ? 1 : 0;

            // 5. 현상금 사냥꾼
            count += CreateEvent("we_bounty_board", "현상금 게시판", "마을 밖 현상금 게시판. 위험한 몬스터를 처치하면 큰 보상!",
                WorldEventType.BountyTarget, WorldEventRarity.Uncommon,
                0.09f, 40f, 3, 99, false, false, WeatherCondition.Any,
                300f, true, 25f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("demon_elite", 1, 112, 118, true, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1500, 1f),
                    Reward(WorldRewardType.Experience, 800, 1f)
                },
                dialogues: new WorldEventDialogue[] {
                    Dialogue("현상금 게시판", "[현상 수배] 위험 등급 A\n강력한 악마가 인근에 출몰. 처치 시 현상금 지급.", 0)
                }) ? 1 : 0;

            return count;
        }

        // ===================== 환경 이벤트 7종 =====================
        private static int GenerateEnvironmentEvents()
        {
            int count = 0;

            // 1. 기상 이변 - 폭풍
            count += CreateEvent("we_weather_storm", "기상 이변: 번개 폭풍", "갑작스런 번개 폭풍이 몰아친다! 번개 원소 몬스터가 출현!",
                WorldEventType.WeatherAnomaly, WorldEventRarity.Uncommon,
                0.12f, 35f, 2, 99, false, false, WeatherCondition.Storm,
                90f, false, 20f, true, "번개 폭풍이 몰려옵니다!",
                monsters: new WorldEventMonster[] {
                    Monster("elemental_normal", 3, 104, 110, false, 0),
                    Monster("elemental_elite", 1, 110, 116, false, 5f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 400, 1f),
                    Reward(WorldRewardType.Experience, 350, 1f)
                }) ? 1 : 0;

            // 2. 기상 이변 - 눈보라
            count += CreateEvent("we_weather_blizzard", "기상 이변: 눈보라", "매서운 눈보라가 몰아친다! 얼음 원소가 깨어났다!",
                WorldEventType.WeatherAnomaly, WorldEventRarity.Uncommon,
                0.10f, 40f, 3, 99, false, false, WeatherCondition.Snow,
                90f, false, 18f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("elemental_normal", 4, 103, 109, false, 0),
                    Monster("elemental_shaman", 1, 108, 114, false, 4f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 350, 1f),
                    Reward(WorldRewardType.Experience, 300, 1f)
                }) ? 1 : 0;

            // 3. 고대 제단 발견
            count += CreateEvent("we_ancient_altar", "고대 제단", "필드에서 고대 제단이 발견되었다. 제물을 바치면 축복이나 저주를 받는다.",
                WorldEventType.AncientAltar, WorldEventRarity.Rare,
                0.05f, 60f, 4, 99, false, false, WeatherCondition.Any,
                120f, true, 5f, false, "",
                dialogues: new WorldEventDialogue[] {
                    Dialogue("고대 제단", "어둠과 빛이 공존하는 제단. 무엇을 바칠 것인가?", 0)
                },
                choices: new WorldEventChoice[] {
                    Choice("골드를 바친다 (300G)", "제단이 빛나며 축복을 내린다!", WorldRewardType.Buff, 1, 0.1f, 50),
                    Choice("피를 바친다", "강력한 힘을 얻었지만 대가가 크다!", WorldRewardType.Experience, 1000, 0.5f, 150),
                    Choice("무시한다", "제단이 조용히 어둠에 잠긴다.", WorldRewardType.Gold, 0, 0f, 0)
                }) ? 1 : 0;

            // 4. 원소 균열
            count += CreateEvent("we_elemental_rift", "원소 균열", "공간에 균열이 생겨 원소 에너지가 분출되고 있다!",
                WorldEventType.ElementalRift, WorldEventRarity.Uncommon,
                0.07f, 45f, 4, 99, false, false, WeatherCondition.Any,
                90f, false, 12f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("elemental_normal", 5, 105, 112, false, 0),
                    Monster("elemental_berserker", 2, 108, 114, false, 3f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 500, 1f),
                    Reward(WorldRewardType.Experience, 450, 1f)
                }) ? 1 : 0;

            // 5. 떨어진 별
            count += CreateEvent("we_fallen_star", "떨어진 별", "하늘에서 빛나는 별이 떨어졌다! 운석 주변에 희귀 자원이 있을지도!",
                WorldEventType.FallenStar, WorldEventRarity.Rare,
                0.03f, 90f, 5, 99, true, false, WeatherCondition.Clear,
                60f, false, 10f, true, "하늘에서 별이 떨어졌습니다!",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 2000, 1f),
                    Reward(WorldRewardType.Experience, 800, 1f),
                    Reward(WorldRewardType.Item, 1, 0.4f, "CrystalStaff_Epic")
                },
                choices: new WorldEventChoice[] {
                    Choice("운석을 조사한다", "희귀 광석을 발견했다!", WorldRewardType.Gold, 1500, 0.2f, 100),
                    Choice("별의 에너지를 흡수한다", "강력한 에너지가 몸에 스며든다!", WorldRewardType.Experience, 1200, 0.3f, 120)
                }) ? 1 : 0;

            // 6. 비 온 뒤 무지개 - 축복
            count += CreateEvent("we_rainbow_blessing", "무지개 축복", "비가 그친 뒤 무지개가 떴다! 무지개 끝에서 축복이 기다린다.",
                WorldEventType.WeatherAnomaly, WorldEventRarity.Rare,
                0.06f, 60f, 1, 99, false, true, WeatherCondition.Rain,
                45f, false, 15f, false, "",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 800, 1f),
                    Reward(WorldRewardType.Buff, 1, 1f)
                }) ? 1 : 0;

            // 7. 피의 달 - 야간 강화
            count += CreateEvent("we_blood_moon", "피의 달", "붉은 달이 떴다! 모든 몬스터가 강화되지만 보상도 2배!",
                WorldEventType.NightHaunt, WorldEventRarity.Rare,
                0.04f, 90f, 5, 99, true, false, WeatherCondition.Any,
                180f, false, 30f, true, "피의 달이 떴습니다! 몬스터가 강화됩니다!",
                monsters: new WorldEventMonster[] {
                    Monster("undead_normal", 4, 108, 114, false, 0),
                    Monster("demon_normal", 3, 110, 116, false, 5f),
                    Monster("undead_boss", 1, 115, 120, true, 15f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 2000, 1f),
                    Reward(WorldRewardType.Experience, 1500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "Greatsword_Rare")
                }) ? 1 : 0;

            return count;
        }

        // ===================== 보물 발견 5종 =====================
        private static int GenerateTreasureDiscoveryEvents()
        {
            int count = 0;

            // 1. 숨겨진 상자
            count += CreateEvent("we_hidden_chest", "숨겨진 상자", "수풀 속에 오래된 상자가 숨겨져 있다!",
                WorldEventType.TreasureDiscovery, WorldEventRarity.Common,
                0.12f, 30f, 1, 99, false, false, WeatherCondition.Any,
                30f, false, 5f, false, "",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 200, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "HealthPotion_Small")
                },
                choices: new WorldEventChoice[] {
                    Choice("열어본다", "소소한 보물이 들어있다!", WorldRewardType.Gold, 200, 0.15f, 30),
                    Choice("함정을 확인한다", "안전하게 열었다!", WorldRewardType.Gold, 150, 0f, 0)
                }) ? 1 : 0;

            // 2. 고대 유적 입구
            count += CreateEvent("we_ancient_ruins", "고대 유적 입구", "땅 속에서 고대 유적의 입구가 드러났다! 보물이 기다리고 있을지도...",
                WorldEventType.TreasureDiscovery, WorldEventRarity.Uncommon,
                0.06f, 50f, 4, 99, false, false, WeatherCondition.Any,
                60f, false, 10f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("construct_normal", 2, 105, 110, false, 0),
                    Monster("undead_normal", 3, 103, 108, false, 3f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 800, 1f),
                    Reward(WorldRewardType.Experience, 400, 1f),
                    Reward(WorldRewardType.Item, 1, 0.25f, "Helmet_Rare")
                }) ? 1 : 0;

            // 3. 해적 매장지
            count += CreateEvent("we_pirate_cache", "해적 매장지", "풍화된 지도가 가리킨 곳! 해적의 보물이 묻혀있다!",
                WorldEventType.TreasureDiscovery, WorldEventRarity.Rare,
                0.04f, 60f, 3, 99, false, false, WeatherCondition.Any,
                45f, false, 8f, false, "",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.35f, "CurvedDagger_Rare")
                },
                choices: new WorldEventChoice[] {
                    Choice("조심스럽게 발굴한다", "보물의 일부를 안전하게 확보했다!", WorldRewardType.Gold, 1000, 0f, 0),
                    Choice("전부 파낸다", "대량의 보물! 하지만 함정이!", WorldRewardType.Gold, 2000, 0.4f, 100)
                }) ? 1 : 0;

            // 4. 마법사의 서재
            count += CreateEvent("we_mage_library", "마법사의 서재", "폐허 속에서 고대 마법사의 서재를 발견했다! 마법서가 가득하다!",
                WorldEventType.TreasureDiscovery, WorldEventRarity.Rare,
                0.03f, 70f, 5, 99, false, false, WeatherCondition.Any,
                60f, false, 6f, false, "",
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Experience, 1000, 1f),
                    Reward(WorldRewardType.SkillPoint, 1, 0.2f)
                },
                choices: new WorldEventChoice[] {
                    Choice("마법서를 읽는다", "강력한 마법 지식을 얻었다!", WorldRewardType.Experience, 1000, 0.15f, 80),
                    Choice("마법서를 가져간다", "귀중한 마법서를 확보했다!", WorldRewardType.Gold, 800, 0.1f, 60)
                }) ? 1 : 0;

            // 5. 드워프 금고
            count += CreateEvent("we_dwarf_vault", "드워프 금고", "산 속에서 드워프의 잊혀진 금고를 발견했다! 엄청난 재화가 저장되어 있다!",
                WorldEventType.TreasureDiscovery, WorldEventRarity.Epic,
                0.02f, 120f, 7, 99, false, false, WeatherCondition.Any,
                90f, false, 10f, true, "드워프 금고가 발견되었습니다!",
                monsters: new WorldEventMonster[] {
                    Monster("construct_elite", 2, 110, 116, false, 0),
                    Monster("construct_boss", 1, 115, 120, true, 5f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 5000, 1f),
                    Reward(WorldRewardType.Experience, 1500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.4f, "Warhammer_Epic")
                }) ? 1 : 0;

            return count;
        }

        // ===================== 미니 퀘스트 5종 =====================
        private static int GenerateMiniQuestEvents()
        {
            int count = 0;

            // 1. 미니보스 토벌
            count += CreateEvent("we_miniboss_hunt", "미니보스 토벌", "강력한 미니보스가 필드에 출현했다! 처치하면 풍성한 보상!",
                WorldEventType.MiniBoss, WorldEventRarity.Uncommon,
                0.08f, 45f, 3, 99, false, false, WeatherCondition.Any,
                120f, true, 15f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("orc_boss", 1, 110, 118, true, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1000, 1f),
                    Reward(WorldRewardType.Experience, 700, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "Battleaxe_Rare")
                }) ? 1 : 0;

            // 2. 현상금 대상 처치
            count += CreateEvent("we_bounty_target", "현상금 대상", "현상 수배 중인 위험한 몬스터가 근처에 있다! 처치하면 현상금 지급!",
                WorldEventType.BountyTarget, WorldEventRarity.Uncommon,
                0.07f, 50f, 4, 99, false, false, WeatherCondition.Any,
                180f, true, 20f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("beast_boss", 1, 112, 118, true, 0),
                    Monster("beast_normal", 2, 105, 110, false, 3f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 1200, 1f),
                    Reward(WorldRewardType.Experience, 600, 1f)
                }) ? 1 : 0;

            // 3. 구출 의뢰
            count += CreateEvent("we_rescue_mission", "구출 의뢰", "상인이 산적에게 납치되었다! 구출하면 큰 보상을 받을 수 있다!",
                WorldEventType.MerchantCaravan, WorldEventRarity.Uncommon,
                0.06f, 55f, 2, 99, false, false, WeatherCondition.Any,
                120f, true, 15f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("goblin_normal", 4, 102, 108, false, 0),
                    Monster("goblin_leader", 1, 108, 114, false, 5f)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Gold, 800, 1f),
                    Reward(WorldRewardType.Experience, 500, 1f),
                    Reward(WorldRewardType.Item, 1, 0.3f, "HealthPotion_Large")
                },
                dialogues: new WorldEventDialogue[] {
                    Dialogue("상인", "살려주세요! 산적들에게 잡혀서...", 0),
                    Dialogue("상인", "감사합니다! 은혜를 잊지 않겠습니다!", 1)
                }) ? 1 : 0;

            // 4. 수호자의 시련
            count += CreateEvent("we_guardian_trial", "수호자의 시련", "고대 수호자의 영혼이 나타나 시련을 제안한다. 통과하면 큰 힘을 얻는다!",
                WorldEventType.MiniBoss, WorldEventRarity.Rare,
                0.04f, 60f, 6, 99, false, false, WeatherCondition.Any,
                150f, true, 12f, false, "",
                monsters: new WorldEventMonster[] {
                    Monster("construct_boss", 1, 114, 120, true, 0)
                },
                rewards: new WorldEventReward[] {
                    Reward(WorldRewardType.Experience, 1500, 1f),
                    Reward(WorldRewardType.Buff, 1, 1f),
                    Reward(WorldRewardType.SkillPoint, 1, 0.15f)
                },
                dialogues: new WorldEventDialogue[] {
                    Dialogue("수호자", "오래전 이 땅을 지키던 자... 그대의 실력을 보여달라.", 0),
                    Dialogue("수호자", "훌륭하다. 그대는 진정한 전사로구나. 내 힘을 물려받아라.", 1)
                }) ? 1 : 0;

            // 5. 도박사의 도전
            count += CreateEvent("we_gambler_challenge", "도박사의 도전", "도박사가 나타나 내기를 제안한다. 운이 좋으면 큰돈, 나쁘면 손실!",
                WorldEventType.MysteriousNPC, WorldEventRarity.Uncommon,
                0.09f, 40f, 2, 99, false, false, WeatherCondition.Any,
                60f, false, 5f, false, "",
                dialogues: new WorldEventDialogue[] {
                    Dialogue("도박사", "하하! 운을 시험해보지 않겠나? 내기 한판 어때?", 0),
                    Dialogue("도박사", "자, 결과를 보자고!", 1)
                },
                choices: new WorldEventChoice[] {
                    Choice("100골드를 건다", "운이 좋으면 3배! 나쁘면 전액 손실!", WorldRewardType.Gold, 300, 0.5f, 0),
                    Choice("500골드를 건다", "대박 아니면 쪽박!", WorldRewardType.Gold, 1500, 0.6f, 0),
                    Choice("거절한다", "현명한 선택이야.", WorldRewardType.Gold, 0, 0f, 0)
                }) ? 1 : 0;

            return count;
        }

        // ===================== 헬퍼 메서드 =====================

        private static bool CreateEvent(string id, string name, string desc,
            WorldEventType type, WorldEventRarity rarity,
            float spawnChance, float checkInterval, int minLevel, int maxLevel,
            bool requiresNight, bool requiresDay, WeatherCondition weather,
            float duration, bool isPermanent, float spawnRadius,
            bool announceToAll, string announceMessage,
            WorldEventMonster[] monsters = null,
            WorldEventReward[] rewards = null,
            WorldEventDialogue[] dialogues = null,
            WorldEventChoice[] choices = null)
        {
            string assetPath = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<WorldEventData>(assetPath) != null) return false;

            var data = ScriptableObject.CreateInstance<WorldEventData>();
            var so = new SerializedObject(data);

            so.FindProperty("eventId").stringValue = id;
            so.FindProperty("eventName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("eventType").enumValueIndex = (int)type;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("spawnChance").floatValue = spawnChance;
            so.FindProperty("checkInterval").floatValue = checkInterval;
            so.FindProperty("minPlayerLevel").intValue = minLevel;
            so.FindProperty("maxPlayerLevel").intValue = maxLevel;
            so.FindProperty("requiresNight").boolValue = requiresNight;
            so.FindProperty("requiresDay").boolValue = requiresDay;
            so.FindProperty("requiredWeather").enumValueIndex = (int)weather;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("isPermanentUntilCompleted").boolValue = isPermanent;
            so.FindProperty("spawnRadius").floatValue = spawnRadius;
            so.FindProperty("announceToAll").boolValue = announceToAll;
            so.FindProperty("announceMessage").stringValue = announceMessage ?? "";

            // Arrays via SerializedProperty
            if (monsters != null) SetMonsterArray(so, monsters);
            if (rewards != null) SetRewardArray(so, rewards);
            if (dialogues != null) SetDialogueArray(so, dialogues);
            if (choices != null) SetChoiceArray(so, choices);

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(data, assetPath);
            return true;
        }

        private static void SetMonsterArray(SerializedObject so, WorldEventMonster[] monsters)
        {
            var prop = so.FindProperty("eventMonsters");
            prop.arraySize = monsters.Length;
            for (int i = 0; i < monsters.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("monsterVariantId").stringValue = monsters[i].monsterVariantId;
                elem.FindPropertyRelative("count").intValue = monsters[i].count;
                elem.FindPropertyRelative("gradeMin").floatValue = monsters[i].gradeMin;
                elem.FindPropertyRelative("gradeMax").floatValue = monsters[i].gradeMax;
                elem.FindPropertyRelative("isBoss").boolValue = monsters[i].isBoss;
                elem.FindPropertyRelative("spawnDelay").floatValue = monsters[i].spawnDelay;
            }
        }

        private static void SetRewardArray(SerializedObject so, WorldEventReward[] rewards)
        {
            var prop = so.FindProperty("rewards");
            prop.arraySize = rewards.Length;
            for (int i = 0; i < rewards.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("rewardType").enumValueIndex = (int)rewards[i].rewardType;
                elem.FindPropertyRelative("amount").intValue = rewards[i].amount;
                elem.FindPropertyRelative("itemId").stringValue = rewards[i].itemId ?? "";
                elem.FindPropertyRelative("dropChance").floatValue = rewards[i].dropChance;
            }
        }

        private static void SetDialogueArray(SerializedObject so, WorldEventDialogue[] dialogues)
        {
            var prop = so.FindProperty("dialogues");
            prop.arraySize = dialogues.Length;
            for (int i = 0; i < dialogues.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("speakerName").stringValue = dialogues[i].speakerName;
                elem.FindPropertyRelative("dialogue").stringValue = dialogues[i].dialogue;
                elem.FindPropertyRelative("order").intValue = dialogues[i].order;
            }
        }

        private static void SetChoiceArray(SerializedObject so, WorldEventChoice[] choices)
        {
            var prop = so.FindProperty("choices");
            prop.arraySize = choices.Length;
            for (int i = 0; i < choices.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("choiceText").stringValue = choices[i].choiceText;
                elem.FindPropertyRelative("resultDescription").stringValue = choices[i].resultDescription;
                elem.FindPropertyRelative("rewardType").enumValueIndex = (int)choices[i].rewardType;
                elem.FindPropertyRelative("rewardAmount").intValue = choices[i].rewardAmount;
                elem.FindPropertyRelative("riskChance").floatValue = choices[i].riskChance;
                elem.FindPropertyRelative("riskDamage").intValue = choices[i].riskDamage;
            }
        }

        // ===================== 팩토리 메서드 =====================

        private static WorldEventMonster Monster(string variantId, int count, float gradeMin, float gradeMax, bool isBoss, float delay)
        {
            return new WorldEventMonster {
                monsterVariantId = variantId,
                count = count,
                gradeMin = gradeMin,
                gradeMax = gradeMax,
                isBoss = isBoss,
                spawnDelay = delay
            };
        }

        private static WorldEventReward Reward(WorldRewardType type, int amount, float chance, string itemId = "")
        {
            return new WorldEventReward {
                rewardType = type,
                amount = amount,
                dropChance = chance,
                itemId = itemId
            };
        }

        private static WorldEventDialogue Dialogue(string speaker, string text, int order)
        {
            return new WorldEventDialogue {
                speakerName = speaker,
                dialogue = text,
                order = order
            };
        }

        private static WorldEventChoice Choice(string text, string result, WorldRewardType rewardType, int rewardAmount, float risk, int riskDmg)
        {
            return new WorldEventChoice {
                choiceText = text,
                resultDescription = result,
                rewardType = rewardType,
                rewardAmount = rewardAmount,
                riskChance = risk,
                riskDamage = riskDmg
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
