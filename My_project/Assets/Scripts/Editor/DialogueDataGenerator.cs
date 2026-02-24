using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Unity.Template.Multiplayer.NGO.Runtime;

namespace Unity.Template.Multiplayer.NGO.Editor
{
    /// <summary>
    /// NPC 대화 데이터 30+개 자동 생성
    /// </summary>
    public class DialogueDataGenerator : EditorWindow
    {
        [MenuItem("Dungeon Crawler/Generate Dialogue Data (30+)")]
        public static void Generate()
        {
            string basePath = "Assets/Resources/ScriptableObjects/Dialogues";
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            int count = 0;

            // ===== 마을 NPC 대화 =====

            // 1. 촌장 - 기본 인사
            count += CreateDialogue(basePath, "elder_greeting", "촌장 알도르", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "촌장 알도르", "어서 오게, 모험가여. 이 마을에 온 것을 환영하네.", "n2"),
                    N("n1", "촌장 알도르", "이 던전 크롤러의 세계는 위험으로 가득하지만, 용감한 자에게는 보상이 기다리고 있다네.", "n3"),
                    N("n3", "촌장 알도르", "마을의 상인들과 대화하고, 장비를 갖추어 던전에 도전해보게!",
                      null, DialogueEffectType.GiveGold, 100, "")
                });

            // 2. 촌장 - 레벨 5 이상
            count += CreateDialogue(basePath, "elder_midlevel", "촌장 알도르", 5,
                CondMinLevel(5),
                new DialogueNode[]
                {
                    N("n1", "촌장 알도르", "벌써 이렇게 성장했구먼! 대단하네.", "n2"),
                    NC("n2", "촌장 알도르", "더 도전하고 싶다면, 어둠의 숲 던전을 추천하네. 보상이 훨씬 좋지.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("도전하겠습니다!", "n3", DialogueEffectType.None, 0, ""),
                            ("아직 준비가 안 됐어요.", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "촌장 알도르", "좋은 선택이네! 건강히 돌아오게.", null),
                    N("n4", "촌장 알도르", "서두를 필요 없네. 충분히 준비하고 가게.", null)
                });

            // 3. 무기 상인
            count += CreateDialogue(basePath, "weapon_merchant", "무기 상인 하그리드", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "하그리드", "어서 와! 최고의 무기를 찾고 있다면 잘 찾아왔어!", "n2"),
                    NC("n2", "하그리드", "뭘 찾고 있나?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("상점 보기", "n3", DialogueEffectType.OpenShop, 0, "weapon"),
                            ("그냥 구경하러 왔어요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "하그리드", "좋은 물건 골라봐!", null),
                    N("n4", "하그리드", "천천히 둘러봐. 뭔가 마음에 드는 게 있으면 말해!", null)
                });

            // 4. 방어구 상인
            count += CreateDialogue(basePath, "armor_merchant", "방어구 상인 헬가", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "헬가", "튼튼한 방어구 없이 던전에 들어가면 죽음뿐이야.", "n2"),
                    N("n2", "헬가", "내 물건은 최고 품질이야. 보고 갈래?",
                        null, DialogueEffectType.OpenShop, 0, "armor")
                });

            // 5. 포션 상인
            count += CreateDialogue(basePath, "potion_merchant", "약사 미란다", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "미란다", "어머, 손님! 체력 포션? 마나 포션? 뭐든 있어요~", "n2"),
                    NC("n2", "미란다", "무엇을 원하시나요?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("물건 보기", "n3", DialogueEffectType.OpenShop, 0, "potion"),
                            ("치유해주세요", "n4", DialogueEffectType.HealPlayer, 0, ""),
                            ("그냥 왔어요", "n5", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "미란다", "좋은 선택! 포션 없이 던전은 자살행위예요!", null),
                    N("n4", "미란다", "자, 깨끗이 치료해드렸어요! 건강하세요~", null),
                    N("n5", "미란다", "그래도 포션은 항상 넉넉히 챙기세요!", null)
                });

            // 6. 대장장이
            count += CreateDialogue(basePath, "blacksmith_greeting", "대장장이 볼칸", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "볼칸", "쾅! 쾅! ...아, 손님이었나. 뭘 강화하고 싶은 거야?", "n2"),
                    NC("n2", "볼칸", "내가 할 수 있는 건 장비 강화야. 골드만 충분하면 돼.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("강화할게요", "n3", DialogueEffectType.OpenCrafting, 0, "enhance"),
                            ("나중에 올게요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "볼칸", "좋아, 어디 보자... 강화할 장비를 골라봐.", null),
                    N("n4", "볼칸", "좋은 장비가 생기면 다시 와.", null)
                });

            // 7. 스킬 마스터 (일반)
            count += CreateDialogue(basePath, "skillmaster_greeting", "스킬 마스터", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "스킬 마스터", "새로운 기술을 배우러 왔나? 올바른 선택이야.", "n2"),
                    N("n2", "스킬 마스터", "골드를 지불하면 너의 직업에 맞는 강력한 스킬을 가르쳐 주지.", "n3"),
                    N("n3", "스킬 마스터", "하지만 한 번 선택한 스킬은 바꿀 수 없으니 신중하게 골라!", null)
                });

            // 8. 던전 가이드
            count += CreateDialogue(basePath, "dungeon_guide", "던전 가이드 피오나", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "피오나", "던전에 대해 알고 싶나요? 제가 안내해 드릴게요!", "n2"),
                    NC("n2", "피오나", "어떤 정보가 필요하세요?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("초보자 던전은?", "n3", DialogueEffectType.None, 0, ""),
                            ("최고급 던전은?", "n4", DialogueEffectType.None, 0, ""),
                            ("월드보스는 뭔가요?", "n5", DialogueEffectType.None, 0, ""),
                            ("됐어요", "n6", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "피오나", "고블린 동굴이 좋아요! 레벨 1~5 모험가에게 딱이에요. 10층으로 되어있고, 5층과 10층에 보스가 있어요.", null),
                    N("n4", "피오나", "마왕의 영역은 레벨 10 이상 전용이에요. 부활도 불가능하니 최고의 장비를 갖추고 가세요!", null),
                    N("n5", "피오나", "월드보스는 30분마다 출현해요. 서버의 모든 플레이어가 함께 싸울 수 있고, 기여도에 따라 보상이 달라져요!", null),
                    N("n6", "피오나", "언제든 물어보세요!", null)
                });

            // 9. 종족별 원로 - 인간
            count += CreateDialogue(basePath, "human_elder", "인간 원로 셀레스", 0,
                CondRace(Race.Human),
                new DialogueNode[]
                {
                    N("n1", "셀레스", "인간의 용사여, 우리 종족의 자부심이로군.", "n2"),
                    N("n2", "셀레스", "인간은 균형 잡힌 능력을 가지고 있어. 어떤 직업이든 훌륭하게 해낼 수 있지.", "n3"),
                    N("n3", "셀레스", "특히 검과 방패를 든 수호기사나, 양손검의 성기사가 우리의 전통이란다.", null)
                });

            // 10. 종족별 원로 - 엘프
            count += CreateDialogue(basePath, "elf_elder", "엘프 현자 일루바타", 0,
                CondRace(Race.Elf),
                new DialogueNode[]
                {
                    N("n1", "일루바타", "별빛의 아이여, 엘프의 긴 역사가 너와 함께하리라.", "n2"),
                    N("n2", "일루바타", "우리 엘프는 마법과 활에 뛰어나지. 지팡이나 활을 잡으면 그 진가를 발휘할 수 있을 거야.", null)
                });

            // 11. 종족별 원로 - 야수족
            count += CreateDialogue(basePath, "beast_elder", "야수 족장 그로크", 0,
                CondRace(Race.Beast),
                new DialogueNode[]
                {
                    N("n1", "그로크", "크하하! 야수족 전사가 왔구나!", "n2"),
                    N("n2", "그로크", "우리의 힘은 맨주먹에서 나온다! 하지만 양손 도끼나 대검도 우리 힘을 잘 살려주지.", null)
                });

            // 12. 종족별 원로 - 마키나
            count += CreateDialogue(basePath, "machina_elder", "마키나 장로 제로원", 0,
                CondRace(Race.Machina),
                new DialogueNode[]
                {
                    N("n1", "제로원", "계산 완료. 마키나 유닛이 탐지되었습니다.", "n2"),
                    N("n2", "제로원", "마키나는 방어력과 안정성이 뛰어납니다. 기계식 무기와의 호환성이 최적입니다.", null)
                });

            // 13-18. 퀘스트 NPC 대화
            count += CreateDialogue(basePath, "quest_guard", "경비병 마르코", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "마르코", "마을 주변에 몬스터가 늘어나고 있소. 도와줄 수 있겠소?", "n2"),
                    NC("n2", "마르코", "고블린 10마리를 처치해주시오.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("맡기세요!", "n3", DialogueEffectType.AcceptQuest, 0, "quest_goblin_10"),
                            ("지금은 힘들어요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "마르코", "고맙소! 고블린은 마을 북쪽에서 출몰하오.", null),
                    N("n4", "마르코", "준비가 되면 다시 와주시오.", null)
                });

            count += CreateDialogue(basePath, "quest_hunter", "사냥꾼 레나", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "레나", "숲에서 강력한 야수가 발견됐어요. 함께 사냥하실래요?", "n2"),
                    NC("n2", "레나", "야수 사냥 퀘스트를 수락하시겠어요?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("네!", "n3", DialogueEffectType.AcceptQuest, 0, "quest_beast_hunt"),
                            ("아니요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "레나", "좋아요! 어둠의 숲 2층에서 만나요!", null),
                    N("n4", "레나", "조심하세요. 숲은 위험해요.", null)
                });

            count += CreateDialogue(basePath, "quest_scholar", "학자 에디슨", 0,
                CondMinLevel(3),
                new DialogueNode[]
                {
                    N("n1", "에디슨", "흥미로운 발견을 했어! 고대 유적에서 특별한 광물이 나온다는 거야.", "n2"),
                    NC("n2", "에디슨", "광물 5개만 가져다주면 보상을 후하게 주겠네.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("해볼게요", "n3", DialogueEffectType.AcceptQuest, 0, "quest_gather_ore"),
                            ("관심 없어요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "에디슨", "고마워! 광석은 고블린 동굴 5층 이후에서 채굴할 수 있어.", null),
                    N("n4", "에디슨", "아쉽군... 다른 모험가를 찾아봐야겠어.", null)
                });

            count += CreateDialogue(basePath, "quest_priestess", "사제 아우렐리아", 0,
                CondMinLevel(5),
                new DialogueNode[]
                {
                    N("n1", "아우렐리아", "성스러운 빛이 어둠에 침식당하고 있어요...", "n2"),
                    N("n2", "아우렐리아", "언데드 지하묘지를 정화해 주실 수 있나요?", "n3"),
                    NC("n3", "아우렐리아", "보스 몬스터를 처치하면 저주가 풀릴 거예요.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("정화하겠습니다", "n4", DialogueEffectType.AcceptQuest, 0, "quest_undead_purge"),
                            ("위험하군요...", "n5", DialogueEffectType.None, 0, "")
                        }),
                    N("n4", "아우렐리아", "신의 가호가 함께하길!", null, DialogueEffectType.HealPlayer, 0, ""),
                    N("n5", "아우렐리아", "충분히 강해지면 다시 와주세요.", null)
                });

            // 19. 길드 마스터
            count += CreateDialogue(basePath, "guild_master", "길드 마스터 바론", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "바론", "모험가 길드에 온 것을 환영한다.", "n2"),
                    NC("n2", "바론", "무엇을 원하나?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("길드에 대해 알려주세요", "n3", DialogueEffectType.None, 0, ""),
                            ("길드를 만들고 싶어요", "n4", DialogueEffectType.None, 0, ""),
                            ("됐습니다", "n5", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "바론", "길드는 함께 모험하는 동료의 모임이지. 레벨 5 이상, 5000G로 창설할 수 있네. 길드 레벨이 오르면 버프도 받지.", null),
                    N("n4", "바론", "G키를 눌러 길드 창을 열고, 길드 생성 버튼을 누르게나.", null),
                    N("n5", "바론", "언제든 다시 오게.", null)
                });

            // 20. 경매장 관리인
            count += CreateDialogue(basePath, "auctioneer", "경매인 골디", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "골디", "여기는 경매장이에요! 아이템을 등록하거나 다른 모험가의 물건을 구매할 수 있죠!", "n2"),
                    N("n2", "골디", "Y키로 경매장을 열 수 있어요. 수수료는 판매가의 5%랍니다!", null)
                });

            // 21-25. 로어 NPC
            count += CreateDialogue(basePath, "lore_historian", "역사학자 클리오", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "클리오", "이 세계의 역사에 대해 궁금한 것이 있나요?", "n2"),
                    NC("n2", "클리오", "어떤 이야기를 듣고 싶으세요?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("4종족의 기원", "n3", DialogueEffectType.None, 0, ""),
                            ("던전의 비밀", "n4", DialogueEffectType.None, 0, ""),
                            ("마왕에 대해", "n5", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "클리오", "인간, 엘프, 야수족, 마키나. 네 종족은 태초의 신이 각각 다른 원소에서 창조했다고 전해집니다. 인간은 흙에서, 엘프는 빛에서, 야수족은 불에서, 마키나는 번개에서...", null),
                    N("n4", "클리오", "던전은 고대 문명의 유적이라는 설이 유력합니다. 어둠의 힘이 응축되면서 몬스터가 생겨난 거죠. 던전이 깊을수록 태고의 힘에 가까워집니다.", null),
                    N("n5", "클리오", "마왕 아자젤... 수천 년 전 어둠의 차원에서 추방된 존재입니다. 주기적으로 현세에 강림하여 파괴를 일으키죠. 그를 막는 것이 모험가들의 궁극적 사명입니다.", null)
                });

            count += CreateDialogue(basePath, "lore_old_knight", "은퇴 기사 베오울프", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "베오울프", "옛날에는 나도 너 같은 모험가였지... 무릎에 화살을 맞기 전까지는.", "n2"),
                    N("n2", "베오울프", "충고 하나 해주마. 장비 강화는 +7까지만 해. 그 이후는 파괴 확률이 있으니까.", "n3"),
                    N("n3", "베오울프", "그리고 룬 소켓도 꼭 활용해. 좋은 룬 하나가 스탯 수십 포인트 값을 한다고.", null)
                });

            count += CreateDialogue(basePath, "lore_wanderer", "방랑자 에텔", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "에텔", "이 세계를 떠도는 것이 내 운명이야.", "n2"),
                    N("n2", "에텔", "먼 곳에서 이상한 소문을 들었어. 드래곤의 둥지에서 전설의 장비가 발견된다는...", "n3"),
                    N("n3", "에텔", "레벨 8 이상이라면 도전해볼 만할 거야. 행운을 빌어!", null)
                });

            count += CreateDialogue(basePath, "lore_mystic", "신비술사 모르가나", 0,
                CondMinLevel(7),
                new DialogueNode[]
                {
                    N("n1", "모르가나", "...별의 움직임이 변하고 있어. 큰 변화가 다가오고 있지.", "n2"),
                    N("n2", "모르가나", "패시브 스킬 트리를 확인해 봤니? P키를 누르면 종족별 고유 패시브를 강화할 수 있어.", "n3"),
                    N("n3", "모르가나", "레벨 10에 도달하면 직업 특성화도 가능해져. 두 갈래 중 하나를 선택해야 하니 신중하게...", null)
                });

            count += CreateDialogue(basePath, "lore_collector", "수집가 펠릭스", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "펠릭스", "아하! 모험가군! 혹시 희귀한 물건을 가지고 있나?", "n2"),
                    NC("n2", "펠릭스", "나는 세계 곳곳의 진귀한 물건을 수집하고 있지.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("어떤 것을 원하세요?", "n3", DialogueEffectType.None, 0, ""),
                            ("관심 없어요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "펠릭스", "장비 세트를 모아봐! 2피스만 모아도 세트 효과가 발동된다고. 전사의 맹세 세트가 초보자에겐 최고야!", null),
                    N("n4", "펠릭스", "언젠가는 좋은 물건을 가져올 거야. 기다리고 있지!", null)
                });

            // 26-30. 특수 NPC 대화
            count += CreateDialogue(basePath, "arena_master", "아레나 마스터 글래디우스", 0,
                CondMinLevel(5),
                new DialogueNode[]
                {
                    N("n1", "글래디우스", "전사의 진정한 가치는 전투에서 증명되는 법!", "n2"),
                    N("n2", "글래디우스", "K키로 아레나에 참가할 수 있다. 1대1 대전에서 승리하여 랭크를 올려보게!", "n3"),
                    N("n3", "글래디우스", "브론즈에서 그랜드마스터까지, 시즌 종료 시 등급에 따른 보상이 있지.", null)
                });

            count += CreateDialogue(basePath, "mount_vendor", "마구간 주인 스테빈", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "스테빈", "탈것이 필요한가? 이동속도가 빨라지면 모험이 훨씬 편해지지!", "n2"),
                    NC("n2", "스테빈", "어떤 탈것이 관심 있나?",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("군마 (2000G)", "n3", DialogueEffectType.None, 0, ""),
                            ("그리폰 (50000G)", "n4", DialogueEffectType.None, 0, ""),
                            ("나중에요", "n5", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "스테빈", "군마는 이동속도 +50%야. 초보 모험가에겐 딱이지!", null),
                    N("n4", "스테빈", "그리폰은 비행이 가능한 전설적인 탈것이야! 하늘을 날 수 있지!", null),
                    N("n5", "스테빈", "마음이 바뀌면 언제든 와!", null)
                });

            count += CreateDialogue(basePath, "pet_trainer", "펫 조련사 루나", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "루나", "안녕! 귀여운 펫을 키워볼래?", "n2"),
                    N("n2", "루나", "펫은 전투, 수집, 버프 타입이 있어. 전투형은 같이 싸우고, 수집형은 아이템을 주워주고, 버프형은 스탯을 올려줘!", "n3"),
                    N("n3", "루나", "펫에게도 경험치를 주면 레벨업하고 더 강해져!", null)
                });

            count += CreateDialogue(basePath, "mail_clerk", "우체국장 헤르메스", 0,
                null,
                new DialogueNode[]
                {
                    N("n1", "헤르메스", "우편이 도착했는지 확인하시겠어요? N키로 우편함을 열 수 있습니다.", "n2"),
                    N("n2", "헤르메스", "다른 모험가에게 골드나 아이템을 보낼 수도 있어요. 발송 비용은 10G입니다!", null)
                });

            count += CreateDialogue(basePath, "craft_master", "제작의 달인 이고르", 0,
                CondMinLevel(3),
                new DialogueNode[]
                {
                    N("n1", "이고르", "제작에 관심이 있나? 재료만 모으면 직접 장비를 만들 수 있지!", "n2"),
                    NC("n2", "이고르", "재료는 채집 노드에서 얻을 수 있어. 광석, 약초, 나무 등이 필요하지.",
                        new (string, string, DialogueEffectType, int, string)[]
                        {
                            ("제작하겠습니다", "n3", DialogueEffectType.OpenCrafting, 0, ""),
                            ("채집 방법을 알려주세요", "n4", DialogueEffectType.None, 0, "")
                        }),
                    N("n3", "이고르", "좋아, 레시피를 골라봐!", null),
                    N("n4", "이고르", "필드에서 반짝이는 노드를 찾아 F키로 상호작용해. 채집 레벨이 올라가면 더 귀한 재료를 얻을 수 있지!", null)
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueGenerator] {count}개 대화 에셋 생성 완료!");
            EditorUtility.DisplayDialog("완료", $"{count}개 대화가 생성되었습니다.", "OK");
        }

        #region 헬퍼

        private static int CreateDialogue(string basePath, string id, string npcName, int priority,
            DialogueCondition condition, DialogueNode[] nodes)
        {
            string path = $"{basePath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<DialogueData>(path) != null)
                return 0;

            var asset = ScriptableObject.CreateInstance<DialogueData>();
            var so = new SerializedObject(asset);

            so.FindProperty("dialogueId").stringValue = id;
            so.FindProperty("npcName").stringValue = npcName;
            so.FindProperty("priority").intValue = priority;

            // 조건
            if (condition != null)
            {
                var condProp = so.FindProperty("condition");
                condProp.FindPropertyRelative("conditionType").enumValueIndex = (int)condition.conditionType;
                condProp.FindPropertyRelative("requiredValue").intValue = condition.requiredValue;
                condProp.FindPropertyRelative("requiredStringValue").stringValue = condition.requiredStringValue ?? "";
                condProp.FindPropertyRelative("requiredRace").enumValueIndex = (int)condition.requiredRace;
            }

            // 노드
            var nodesProp = so.FindProperty("nodes");
            nodesProp.ClearArray();
            for (int i = 0; i < nodes.Length; i++)
            {
                nodesProp.InsertArrayElementAtIndex(i);
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var node = nodes[i];

                nodeProp.FindPropertyRelative("nodeId").stringValue = node.nodeId ?? $"n{i + 1}";
                nodeProp.FindPropertyRelative("speakerName").stringValue = node.speakerName ?? "";
                nodeProp.FindPropertyRelative("text").stringValue = node.text ?? "";
                nodeProp.FindPropertyRelative("nextNodeId").stringValue = node.nextNodeId ?? "";

                // 효과
                if (node.effect != null)
                {
                    var effectProp = nodeProp.FindPropertyRelative("effect");
                    effectProp.FindPropertyRelative("effectType").enumValueIndex = (int)node.effect.effectType;
                    effectProp.FindPropertyRelative("intValue").intValue = node.effect.intValue;
                    effectProp.FindPropertyRelative("stringValue").stringValue = node.effect.stringValue ?? "";
                }

                // 선택지
                if (node.choices != null && node.choices.Count > 0)
                {
                    var choicesProp = nodeProp.FindPropertyRelative("choices");
                    choicesProp.ClearArray();
                    for (int c = 0; c < node.choices.Count; c++)
                    {
                        choicesProp.InsertArrayElementAtIndex(c);
                        var choiceProp = choicesProp.GetArrayElementAtIndex(c);
                        var choice = node.choices[c];

                        choiceProp.FindPropertyRelative("choiceText").stringValue = choice.choiceText ?? "";
                        choiceProp.FindPropertyRelative("nextNodeId").stringValue = choice.nextNodeId ?? "";

                        if (choice.effect != null)
                        {
                            var cEffectProp = choiceProp.FindPropertyRelative("effect");
                            cEffectProp.FindPropertyRelative("effectType").enumValueIndex = (int)choice.effect.effectType;
                            cEffectProp.FindPropertyRelative("intValue").intValue = choice.effect.intValue;
                            cEffectProp.FindPropertyRelative("stringValue").stringValue = choice.effect.stringValue ?? "";
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, path);
            return 1;
        }

        // 간편 노드 생성 (선택지 없음)
        private static DialogueNode N(string id, string speaker, string text, string next,
            DialogueEffectType effectType = DialogueEffectType.None, int effectInt = 0, string effectStr = "")
        {
            var node = new DialogueNode
            {
                nodeId = id,
                speakerName = speaker,
                text = text,
                nextNodeId = next ?? "",
                choices = null
            };

            if (effectType != DialogueEffectType.None)
            {
                node.effect = new DialogueNodeEffect
                {
                    effectType = effectType,
                    intValue = effectInt,
                    stringValue = effectStr ?? ""
                };
            }

            return node;
        }

        // 간편 노드 생성 (선택지 있음)
        private static DialogueNode NC(string id, string speaker, string text,
            (string choiceText, string nextId, DialogueEffectType effectType, int effectInt, string effectStr)[] choices)
        {
            var node = new DialogueNode
            {
                nodeId = id,
                speakerName = speaker,
                text = text,
                nextNodeId = "",
                choices = new List<DialogueChoice>()
            };

            foreach (var c in choices)
            {
                var choice = new DialogueChoice
                {
                    choiceText = c.choiceText,
                    nextNodeId = c.nextId
                };
                if (c.effectType != DialogueEffectType.None)
                {
                    choice.effect = new DialogueNodeEffect
                    {
                        effectType = c.effectType,
                        intValue = c.effectInt,
                        stringValue = c.effectStr ?? ""
                    };
                }
                node.choices.Add(choice);
            }

            return node;
        }

        private static DialogueCondition CondMinLevel(int level)
        {
            return new DialogueCondition { conditionType = DialogueConditionType.MinLevel, requiredValue = level };
        }

        private static DialogueCondition CondRace(Race race)
        {
            return new DialogueCondition { conditionType = DialogueConditionType.RaceIs, requiredRace = race };
        }

        #endregion
    }
}
