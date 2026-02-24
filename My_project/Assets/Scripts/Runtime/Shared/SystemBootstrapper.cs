using UnityEngine;
using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// System Bootstrapper - Ensures all singleton systems are initialized on game start.
    /// Attach to a persistent GameObject in the startup scene.
    /// Creates system GameObjects for any missing singletons.
    /// MonoBehaviour systems: created immediately.
    /// NetworkBehaviour systems: created with NetworkObject and Spawned on server.
    /// </summary>
    public class SystemBootstrapper : MonoBehaviour
    {
        public static SystemBootstrapper Instance { get; private set; }

        [Header("Auto-Create Missing Systems")]
        [SerializeField] private bool autoCreateSystems = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoCreateSystems)
                InitializeSystems();
        }

        /// <summary>
        /// NetworkManager 연결 후 네트워크 시스템 초기화 (호스트/서버 시작 후 호출)
        /// </summary>
        public void InitializeNetworkSystems()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[SystemBootstrapper] Network systems require active server.");
                return;
            }

            Debug.Log("[SystemBootstrapper] Initializing network systems...");

            // === Network Layer 1: Core network systems ===
            EnsureNetworkSystem<SaveSystem>("SaveSystem");
            EnsureNetworkSystem<PlayerSaveSystem>("PlayerSaveSystem");
            EnsureNetworkSystem<DungeonManager>("DungeonManager");
            EnsureNetworkSystem<PartyManager>("PartyManager");

            // === Network Layer 2: Economy & Trade ===
            EnsureNetworkSystem<GamblingSystem>("GamblingSystem");
            EnsureNetworkSystem<TradeSystem>("TradeSystem");
            EnsureNetworkSystem<AuctionSystem>("AuctionSystem");
            EnsureNetworkSystem<MailSystem>("MailSystem");

            // === Network Layer 3: Combat extensions ===
            EnsureNetworkSystem<GemSystem>("GemSystem");
            EnsureNetworkSystem<SalvageSystem>("SalvageSystem");
            EnsureNetworkSystem<TalismanSystem>("TalismanSystem");
            EnsureNetworkSystem<LegendaryAspectSystem>("LegendaryAspectSystem");
            EnsureNetworkSystem<EnchantRerollSystem>("EnchantRerollSystem");
            EnsureNetworkSystem<EquipmentAwakeningSystem>("EquipmentAwakeningSystem");

            // === Network Layer 4: Dungeon extensions ===
            EnsureNetworkSystem<NightmareDungeonSystem>("NightmareDungeonSystem");
            EnsureNetworkSystem<InfernalHordesSystem>("InfernalHordesSystem");
            EnsureNetworkSystem<HiddenRealmSystem>("HiddenRealmSystem");
            EnsureNetworkSystem<EventDungeonSystem>("EventDungeonSystem");
            EnsureNetworkSystem<RiftChallengeSystem>("RiftChallengeSystem");
            EnsureNetworkSystem<WaystoneCraftingSystem>("WaystoneCraftingSystem");
            EnsureNetworkSystem<WorldDifficultySystem>("WorldDifficultySystem");
            EnsureNetworkSystem<PinnacleBossSystem>("PinnacleBossSystem");

            // === Network Layer 5: Content systems ===
            EnsureNetworkSystem<QuestManager>("QuestManager");
            EnsureNetworkSystem<CraftingSystem>("CraftingSystem");
            EnsureNetworkSystem<GuildSystem>("GuildSystem");
            EnsureNetworkSystem<ArenaSystem>("ArenaSystem");
            EnsureNetworkSystem<PetSystem>("PetSystem");
            EnsureNetworkSystem<MountSystem>("MountSystem");
            EnsureNetworkSystem<SeasonPassSystem>("SeasonPassSystem");
            EnsureNetworkSystem<DailyRewardSystem>("DailyRewardSystem");
            EnsureNetworkSystem<HousingSystem>("HousingSystem");

            // === Network Layer 6: Endgame systems ===
            EnsureNetworkSystem<RogueliteSystem>("RogueliteSystem");
            EnsureNetworkSystem<ExpeditionSystem>("ExpeditionSystem");
            EnsureNetworkSystem<BossRushSystem>("BossRushSystem");
            EnsureNetworkSystem<WorldBossSystem>("WorldBossSystem");
            EnsureNetworkSystem<ReputationSystem>("ReputationSystem");

            // === Network Layer 7: Skill extensions ===
            EnsureNetworkSystem<SkillEnhanceSystem>("SkillEnhanceSystem");
            EnsureNetworkSystem<SkillMutationSystem>("SkillMutationSystem");
            EnsureNetworkSystem<SkillSpecializationSystem>("SkillSpecializationSystem");
            EnsureNetworkSystem<PassiveSkillTreeSystem>("PassiveSkillTreeSystem");
            EnsureNetworkSystem<JobSpecializationSystem>("JobSpecializationSystem");
            EnsureNetworkSystem<HeritageSystem>("HeritageSystem");
            EnsureNetworkSystem<LoadoutSystem>("LoadoutSystem");

            // === Network Layer 8: Weather & Environment ===
            EnsureNetworkSystem<WeatherSystem>("WeatherSystem");

            Debug.Log("[SystemBootstrapper] All network systems initialized.");
        }

        private void InitializeSystems()
        {
            // === Layer 1: Core infrastructure (no dependencies) ===
            EnsureSystem<NotificationManager>("NotificationManager");
            EnsureSystem<UIManager>("UIManager");
            EnsureSystem<EffectManager>("EffectManager");
            EnsureSystem<DamageNumberManager>("DamageNumberManager");
            EnsureSystem<SoundManager>("SoundManager");
            EnsureSystem<SettingsManager>("SettingsManager");

            // === Layer 2: Combat foundations ===
            EnsureSystem<PvPBalanceSystem>("PvPBalanceSystem");
            EnsureSystem<CombatSystem>("CombatSystem");
            EnsureSystem<TransformationSystem>("TransformationSystem");
            EnsureSystem<AutoCombatSystem>("AutoCombatSystem");

            // === Layer 3: Player progression ===
            EnsureSystem<NewSkillLearningSystem>("SkillLearningSystem");
            EnsureSystem<WeaponProficiencySystem>("WeaponProficiencySystem");
            EnsureSystem<SkillComboSystem>("SkillComboSystem");

            // === Layer 4: Content tracking (needed by item systems) ===
            EnsureSystem<CodexSystem>("CodexSystem");
            EnsureSystem<ProphecySystem>("ProphecySystem");
            EnsureSystem<AchievementSystem>("AchievementSystem");
            EnsureSystem<CollectionSystem>("CollectionSystem");

            // === Layer 5: Economy ===
            EnsureSystem<EconomySystem>("EconomySystem");

            // === Layer 6: Item systems (depend on ProphecySystem, CodexSystem) ===
            EnsureSystem<InfusionSystem>("InfusionSystem");
            EnsureSystem<TemperingSystem>("TemperingSystem");
            EnsureSystem<CorruptionSystem>("CorruptionSystem");
            EnsureSystem<RelicSystem>("RelicSystem");
            EnsureSystem<ItemEnhanceSystem>("ItemEnhanceSystem");
            EnsureSystem<LootFilterSystem>("LootFilterSystem");
            EnsureSystem<EquipmentSetSystem>("EquipmentSetSystem");

            // === Layer 7: Content systems ===
            EnsureSystem<RitualSystem>("RitualSystem");
            EnsureSystem<BestiaryHuntSystem>("BestiaryHuntSystem");
            EnsureSystem<EliteModifierSystem>("EliteModifierSystem");
            EnsureSystem<DialogueSystem>("DialogueSystem");

            // === Layer 8: Dungeon systems ===
            EnsureSystem<DungeonController>("DungeonController");
            EnsureSystem<DungeonEventManager>("DungeonEventManager");
            EnsureSystem<AtlasPassiveSystem>("AtlasPassiveSystem");
            EnsureSystem<ShrineSystem>("ShrineSystem");
            EnsureSystem<BossRewardSystem>("BossRewardSystem");

            // === Layer 9: Game controller ===
            EnsureSystem<GameController>("GameController");

            Debug.Log("[SystemBootstrapper] All local systems initialized.");
        }

        private T EnsureSystem<T>(string systemName) where T : Component
        {
            try
            {
                var existing = FindFirstObjectByType<T>();
                if (existing != null) return existing;

                var go = new GameObject($"[System] {systemName}");
                go.transform.SetParent(transform);
                var component = go.AddComponent<T>();
                Debug.Log($"[SystemBootstrapper] Created: {systemName}");
                return component;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SystemBootstrapper] FAILED to create {systemName}: {ex.Message}");
                return null;
            }
        }

        private T EnsureNetworkSystem<T>(string systemName) where T : NetworkBehaviour
        {
            try
            {
                var existing = FindFirstObjectByType<T>();
                if (existing != null) return existing;

                var go = new GameObject($"[NetSystem] {systemName}");
                DontDestroyOnLoad(go);
                go.AddComponent<NetworkObject>();
                var component = go.AddComponent<T>();

                // 서버에서만 Spawn
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    go.GetComponent<NetworkObject>().Spawn();
                    Debug.Log($"[SystemBootstrapper] Created & Spawned: {systemName}");
                }

                return component;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SystemBootstrapper] FAILED to create network system {systemName}: {ex.Message}");
                return null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
