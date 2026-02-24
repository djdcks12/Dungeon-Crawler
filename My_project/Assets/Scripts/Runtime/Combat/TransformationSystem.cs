using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public enum TransformationType
    {
        Berserker,
        Shadow,
        Elemental,
        AncientGuardian
    }

    public class TransformationData
    {
        public TransformationType Type;
        public string Name;
        public string Description;
        public Dictionary<string, float> StatBonuses;
        public float Duration;
        public string SpecialSkillName;

        public TransformationData(TransformationType type, string name, string description,
            Dictionary<string, float> statBonuses, float duration, string specialSkillName)
        {
            Type = type;
            Name = name;
            Description = description;
            StatBonuses = statBonuses;
            Duration = duration;
            SpecialSkillName = specialSkillName;
        }
    }

    public struct TransformationState
    {
        public TransformationType Type;
        public float StartTime;
        public float GaugePercent;

        public TransformationState(TransformationType type, float startTime, float gaugePercent)
        {
            Type = type;
            StartTime = startTime;
            GaugePercent = gaugePercent;
        }
    }

    public class TransformationSystem : NetworkBehaviour
    {
        public static TransformationSystem Instance { get; private set; }

        // Events
        public event Action<TransformationType> OnTransformActivated;
        public event Action OnTransformEnded;
        public event Action<float> OnGaugeChanged;

        // Local client state
        public TransformationType? LocalActiveForm { get; private set; }
        public float LocalGauge { get; private set; }
        public float LocalTransformEndTime { get; private set; }

        // Server state
        private Dictionary<ulong, TransformationState> activeTransformations = new Dictionary<ulong, TransformationState>();
        private Dictionary<ulong, float> playerGauges = new Dictionary<ulong, float>();

        // Transformation definitions
        private Dictionary<TransformationType, TransformationData> transformationDatabase;

        // Race synergy mapping
        private static readonly Dictionary<string, TransformationType> raceSynergyMap = new Dictionary<string, TransformationType>
        {
            { "Human", TransformationType.Berserker },
            { "Elf", TransformationType.Shadow },
            { "Dwarf", TransformationType.AncientGuardian },
            { "Machina", TransformationType.Elemental }
        };

        private const float GAUGE_MAX = 100f;
        private const float MAX_GAUGE_PER_CALL = 5f;
        private const float RACE_SYNERGY_BONUS = 0.10f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeTransformationDatabase();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            base.OnDestroy();
        }

        private void InitializeTransformationDatabase()
        {
            transformationDatabase = new Dictionary<TransformationType, TransformationData>
            {
                {
                    TransformationType.Berserker,
                    new TransformationData(
                        TransformationType.Berserker,
                        "Berserker Form",
                        "Unleash primal fury. Massive strength and attack boost at the cost of defense.",
                        new Dictionary<string, float>
                        {
                            { "STR", 0.50f },
                            { "ATK", 0.40f },
                            { "DEF", -0.20f }
                        },
                        30f,
                        "BerserkerRage"
                    )
                },
                {
                    TransformationType.Shadow,
                    new TransformationData(
                        TransformationType.Shadow,
                        "Shadow Form",
                        "Become one with shadows. Extreme agility with lethal critical strikes.",
                        new Dictionary<string, float>
                        {
                            { "AGI", 0.60f },
                            { "CritRate", 0.30f },
                            { "Evasion", 0.25f }
                        },
                        30f,
                        "ShadowStrike"
                    )
                },
                {
                    TransformationType.Elemental,
                    new TransformationData(
                        TransformationType.Elemental,
                        "Elemental Form",
                        "Channel raw elemental power. Devastating spells with enhanced resistances.",
                        new Dictionary<string, float>
                        {
                            { "INT", 0.50f },
                            { "SpellDmg", 0.45f },
                            { "AllResist", 15f }
                        },
                        30f,
                        "ElementalBurst"
                    )
                },
                {
                    TransformationType.AncientGuardian,
                    new TransformationData(
                        TransformationType.AncientGuardian,
                        "Ancient Guardian Form",
                        "Invoke ancient protection. Unbreakable defense with regenerative power.",
                        new Dictionary<string, float>
                        {
                            { "DEF", 0.60f },
                            { "VIT", 0.40f },
                            { "HPRegen", 0.30f }
                        },
                        30f,
                        "GuardianShield"
                    )
                }
            };
        }

        private void Update()
        {
            if (!IsServer) return;

            var expiredClients = new List<ulong>();
            foreach (var kvp in activeTransformations)
            {
                ulong clientId = kvp.Key;
                TransformationState state = kvp.Value;
                if (!transformationDatabase.TryGetValue(state.Type, out TransformationData data)) continue;

                if (Time.time - state.StartTime >= data.Duration)
                {
                    expiredClients.Add(clientId);
                }
            }

            foreach (ulong clientId in expiredClients)
            {
                DeactivateTransform(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ActivateTransformServerRpc(int transformTypeInt, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!System.Enum.IsDefined(typeof(TransformationType), transformTypeInt))
                return;

            TransformationType type = (TransformationType)transformTypeInt;

            if (activeTransformations.ContainsKey(clientId))
                return;

            float gauge = GetGaugeInternal(clientId);
            if (gauge < GAUGE_MAX)
                return;

            playerGauges[clientId] = 0f;

            var state = new TransformationState(type, Time.time, gauge);
            activeTransformations[clientId] = state;

            if (!transformationDatabase.TryGetValue(type, out TransformationData data)) return;
            NotifyTransformActivatedClientRpc(transformTypeInt, data.Name, data.Duration, clientId);
            SyncGaugeClientRpc(0f, clientId);

            NotificationManager.Instance?.ShowNotification(
                $"{data.Name} activated for {data.Duration}s!",
                NotificationType.System);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeactivateTransformServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            DeactivateTransform(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChargeGaugeServerRpc(float amount, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (amount <= 0f || amount > MAX_GAUGE_PER_CALL)
                return;

            if (activeTransformations.ContainsKey(clientId))
                return;

            float current = GetGaugeInternal(clientId);
            float newGauge = Mathf.Min(current + amount, GAUGE_MAX);
            playerGauges[clientId] = newGauge;

            SyncGaugeClientRpc(newGauge, clientId);
        }

        private void DeactivateTransform(ulong clientId)
        {
            if (!activeTransformations.ContainsKey(clientId))
                return;

            activeTransformations.Remove(clientId);
            NotifyTransformEndedClientRpc(clientId);

            NotificationManager.Instance?.ShowNotification(
                "Transformation ended.",
                NotificationType.Warning);
        }

        // --- Public query methods ---

        public Dictionary<string, float> GetTransformBonuses(ulong clientId, string race = null)
        {
            var bonuses = new Dictionary<string, float>();

            if (!activeTransformations.TryGetValue(clientId, out TransformationState state))
                return bonuses;

            if (!transformationDatabase.TryGetValue(state.Type, out TransformationData data))
                return bonuses;

            foreach (var kvp in data.StatBonuses)
            {
                bonuses[kvp.Key] = kvp.Value;
            }

            // Apply race synergy bonus
            if (!string.IsNullOrEmpty(race) && raceSynergyMap.TryGetValue(race, out TransformationType synergyType))
            {
                if (synergyType == state.Type)
                {
                    var keys = new List<string>(bonuses.Keys);
                    foreach (string key in keys)
                    {
                        bonuses[key] += bonuses[key] * RACE_SYNERGY_BONUS;
                    }
                }
            }

            return bonuses;
        }

        public bool IsTransformed(ulong clientId)
        {
            return activeTransformations.ContainsKey(clientId);
        }

        public float GetGauge(ulong clientId)
        {
            return GetGaugeInternal(clientId);
        }

        private float GetGaugeInternal(ulong clientId)
        {
            return playerGauges.TryGetValue(clientId, out float gauge) ? gauge : 0f;
        }

        public TransformationData GetTransformationData(TransformationType type)
        {
            return transformationDatabase.TryGetValue(type, out TransformationData data) ? data : null;
        }

        // --- ClientRpcs ---

        [ClientRpc]
        private void NotifyTransformActivatedClientRpc(int type, string formName, float duration, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                LocalActiveForm = (TransformationType)type;
                LocalTransformEndTime = Time.time + duration;
                OnTransformActivated?.Invoke((TransformationType)type);

                NotificationManager.Instance?.ShowNotification(
                    $"{formName} activated! Duration: {duration}s",
                    NotificationType.System);
            }
        }

        [ClientRpc]
        private void NotifyTransformEndedClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                LocalActiveForm = null;
                LocalTransformEndTime = 0f;
                OnTransformEnded?.Invoke();

                NotificationManager.Instance?.ShowNotification(
                    "Transformation has ended.",
                    NotificationType.Warning);
            }
        }

        [ClientRpc]
        private void SyncGaugeClientRpc(float gauge, ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                LocalGauge = gauge;
                OnGaugeChanged?.Invoke(gauge);
            }
        }

        // --- Gauge helper methods for external systems ---

        public void ChargeGaugeForHitDealt()
        {
            ChargeGaugeServerRpc(1f);
        }

        public void ChargeGaugeForHitTaken()
        {
            ChargeGaugeServerRpc(2f);
        }

        public void ChargeGaugeForSkillUsed()
        {
            ChargeGaugeServerRpc(5f);
        }
    }
}
