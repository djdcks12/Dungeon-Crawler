using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 실제 게임에서 사용되는 아이템 인스턴스
    /// ItemData의 실제 구현체로, 개별 아이템의 상태를 관리
    /// </summary>
    [System.Serializable]
    public class ItemInstance : INetworkSerializable
    {
        [SerializeField] private string itemId = "";
        [SerializeField] private string instanceId = "";
        [SerializeField] private int quantity = 1;
        [SerializeField] private int currentDurability = 100;
        [SerializeField] private long acquisitionTime = 0;
        
        // 인챈트 시스템용 (추후 구현)
        [SerializeField] private string[] enchantments = new string[0];
        
        // 커스텀 데이터 (인챈트 북 등에 사용)
        [SerializeField] private Dictionary<string, string> customData = new Dictionary<string, string>();
        
        // 캐시된 ItemData (네트워크 직렬화하지 않음)
        private ItemData cachedItemData;
        
        // 프로퍼티들
        public string ItemId => itemId;
        public string InstanceId => instanceId;
        public int Quantity => quantity;
        public int CurrentDurability => currentDurability;
        public long AcquisitionTime => acquisitionTime;
        public string[] Enchantments => enchantments;
        public Dictionary<string, string> CustomData => customData;
        
        /// <summary>
        /// ItemData 참조 (캐시됨)
        /// </summary>
        public ItemData ItemData
        {
            get
            {
                if (cachedItemData == null)
                {
                    cachedItemData = ItemDatabase.GetItem(itemId);
                }
                return cachedItemData;
            }
        }
        
        /// <summary>
        /// 빈 생성자 (네트워크 직렬화용)
        /// </summary>
        public ItemInstance()
        {
            instanceId = System.Guid.NewGuid().ToString();
            acquisitionTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            customData = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// ItemData로부터 새 인스턴스 생성
        /// </summary>
        public ItemInstance(ItemData itemData, int quantity = 1)
        {
            this.itemId = itemData.ItemId;
            this.instanceId = System.Guid.NewGuid().ToString();
            this.customData = new Dictionary<string, string>();
            this.quantity = Mathf.Max(1, quantity);
            this.currentDurability = itemData.MaxDurability;
            this.acquisitionTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.cachedItemData = itemData;
        }
        
        /// <summary>
        /// 기존 인스턴스로부터 복사 생성
        /// </summary>
        public ItemInstance(ItemInstance other)
        {
            this.itemId = other.itemId;
            this.instanceId = System.Guid.NewGuid().ToString(); // 새로운 ID 생성
            this.quantity = other.quantity;
            this.currentDurability = other.currentDurability;
            this.acquisitionTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.enchantments = (string[])other.enchantments.Clone();
            this.cachedItemData = other.cachedItemData;
        }
        
        /// <summary>
        /// 아이템이 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(itemId) || ItemData == null)
                return false;
                
            if (quantity <= 0)
                return false;
                
            if (ItemData.IsEquippable && currentDurability < 0)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 아이템이 사용 가능한지 확인
        /// </summary>
        public bool CanUse()
        {
            if (!IsValid()) return false;
            
            // 장비 아이템은 내구도가 0이면 사용 불가
            if (ItemData.IsEquippable && currentDurability <= 0)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 아이템이 스택 가능한지 확인
        /// </summary>
        public bool CanStackWith(ItemInstance other)
        {
            if (other == null || !IsValid() || !other.IsValid())
                return false;
                
            // 같은 아이템 타입이고
            if (itemId != other.itemId)
                return false;
                
            // 스택 사이즈가 1보다 크고
            if (ItemData.StackSize <= 1)
                return false;
                
            // 장비가 아니고 (장비는 개별 내구도를 가짐)
            if (ItemData.IsEquippable)
                return false;
                
            // 인챈트가 동일해야 함
            if (!ArrayEquals(enchantments, other.enchantments))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 다른 아이템과 스택 시도
        /// </summary>
        public bool TryStackWith(ItemInstance other, out int remainingQuantity)
        {
            remainingQuantity = 0;
            
            if (!CanStackWith(other))
                return false;
                
            int maxStack = ItemData.StackSize;
            int totalQuantity = quantity + other.quantity;
            
            if (totalQuantity <= maxStack)
            {
                // 모두 스택 가능
                quantity = totalQuantity;
                return true;
            }
            else
            {
                // 일부만 스택 가능
                quantity = maxStack;
                remainingQuantity = totalQuantity - maxStack;
                return true;
            }
        }
        
        /// <summary>
        /// 수량 분할
        /// </summary>
        public ItemInstance SplitStack(int splitQuantity)
        {
            if (splitQuantity <= 0 || splitQuantity >= quantity)
                return null;
                
            if (ItemData.StackSize <= 1)
                return null;
                
            // 새로운 인스턴스 생성
            var newInstance = new ItemInstance(this);
            newInstance.quantity = splitQuantity;
            
            // 기존 수량 감소
            quantity -= splitQuantity;
            
            return newInstance;
        }
        
        /// <summary>
        /// 내구도 감소
        /// </summary>
        public void DecreaseDurability(int amount = 1)
        {
            if (ItemData.IsEquippable)
            {
                currentDurability = Mathf.Max(0, currentDurability - amount);
            }
        }
        
        /// <summary>
        /// 내구도 수리
        /// </summary>
        public void RepairDurability(int amount)
        {
            if (ItemData.IsEquippable)
            {
                currentDurability = Mathf.Min(ItemData.MaxDurability, currentDurability + amount);
            }
        }
        
        /// <summary>
        /// 아이템 인스턴스 초기화 (ID와 수량 설정)
        /// </summary>
        public void Initialize(string itemId, int quantity = 1)
        {
            this.itemId = itemId;
            this.quantity = Mathf.Max(1, quantity);
            this.instanceId = System.Guid.NewGuid().ToString();
            this.acquisitionTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // ItemData 로드
            cachedItemData = ItemDatabase.GetItem(itemId);
            if (cachedItemData != null)
            {
                this.currentDurability = cachedItemData.MaxDurability;
            }
        }
        
        /// <summary>
        /// 수량 변경
        /// </summary>
        public void ChangeQuantity(int newQuantity)
        {
            quantity = Mathf.Max(0, newQuantity);
        }
        
        /// <summary>
        /// 수량 설정
        /// </summary>
        public void SetQuantity(int newQuantity)
        {
            quantity = Mathf.Max(0, newQuantity);
        }
        
        /// <summary>
        /// 수량 추가
        /// </summary>
        public void AddQuantity(int amount)
        {
            quantity = Mathf.Max(0, quantity + amount);
        }
        
        /// <summary>
        /// 아이템 복사
        /// </summary>
        public ItemInstance Clone()
        {
            var clone = new ItemInstance();
            clone.itemId = this.itemId;
            clone.instanceId = System.Guid.NewGuid().ToString();
            clone.quantity = this.quantity;
            clone.currentDurability = this.currentDurability;
            clone.acquisitionTime = this.acquisitionTime;
            clone.enchantments = (string[])this.enchantments.Clone();
            clone.cachedItemData = this.cachedItemData;
            return clone;
        }
        
        /// <summary>
        /// 내구도 비율 (0.0 ~ 1.0)
        /// </summary>
        public float GetDurabilityPercentage()
        {
            if (ItemData == null || !ItemData.HasDurability) return 1.0f;
            return currentDurability / (float)ItemData.MaxDurability;
        }
        
        /// <summary>
        /// 아이템 수리
        /// </summary>
        public void RepairItem(int repairAmount)
        {
            if (ItemData != null && ItemData.HasDurability)
            {
                currentDurability = Mathf.Min(ItemData.MaxDurability, currentDurability + repairAmount);
            }
        }
        
        /// <summary>
        /// 인챈트 추가 (추후 인챈트 시스템에서 사용)
        /// </summary>
        public bool AddEnchantment(string enchantmentId)
        {
            if (enchantments.Length >= 3) // 최대 3개 인챈트
                return false;
                
            // 중복 체크
            foreach (string existing in enchantments)
            {
                if (existing == enchantmentId)
                    return false;
            }
            
            // 배열 확장
            string[] newEnchantments = new string[enchantments.Length + 1];
            for (int i = 0; i < enchantments.Length; i++)
            {
                newEnchantments[i] = enchantments[i];
            }
            newEnchantments[enchantments.Length] = enchantmentId;
            enchantments = newEnchantments;
            
            return true;
        }
        
        /// <summary>
        /// 현재 무기 데미지 계산 (내구도 및 인챈트 적용)
        /// </summary>
        public DamageRange GetCurrentWeaponDamage(float strength, float stability)
        {
            if (!ItemData.IsWeapon)
                return new DamageRange(0, 0, 0);
                
            // 기본 무기 데미지 계산
            DamageRange baseDamage = ItemData.CalculateWeaponDamage(strength, stability);
            
            // 내구도에 따른 감소
            float durabilityRatio = currentDurability / (float)ItemData.MaxDurability;
            baseDamage.minDamage *= durabilityRatio;
            baseDamage.maxDamage *= durabilityRatio;
            
            // 인챈트 보너스 (추후 구현)
            // TODO: 인챈트 시스템에서 데미지 보너스 적용
            
            return baseDamage;
        }
        
        /// <summary>
        /// 현재 스탯 보너스 계산 (내구도 및 인챈트 적용)
        /// </summary>
        public StatBlock GetCurrentStatBonuses()
        {
            if (!ItemData.IsEquippable)
                return new StatBlock();
                
            StatBlock bonuses = ItemData.StatBonuses;
            
            // 내구도에 따른 감소
            float durabilityRatio = currentDurability / (float)ItemData.MaxDurability;
            bonuses = bonuses * durabilityRatio;
            
            // 인챈트 보너스 (추후 구현)
            // TODO: 인챈트 시스템에서 스탯 보너스 적용
            
            return bonuses;
        }
        
        /// <summary>
        /// 아이템 정보 텍스트 (인스턴스 상태 포함)
        /// </summary>
        public string GetDetailedInfoText()
        {
            if (ItemData == null) return "Invalid Item";
            
            string info = ItemData.GetInfoText();
            
            // 인스턴스별 정보 추가
            if (quantity > 1)
            {
                info += $"\n수량: {quantity}";
            }
            
            if (ItemData.IsEquippable && currentDurability != ItemData.MaxDurability)
            {
                float durabilityPercent = (currentDurability / (float)ItemData.MaxDurability) * 100f;
                info += $"\n현재 내구도: {currentDurability}/{ItemData.MaxDurability} ({durabilityPercent:F1}%)";
            }
            
            if (enchantments.Length > 0)
            {
                info += "\n인챈트:";
                foreach (string enchant in enchantments)
                {
                    info += $"\n  - {enchant}";
                }
            }
            
            // 획득 시간
            DateTime acquisitionDate = DateTimeOffset.FromUnixTimeSeconds(acquisitionTime).DateTime;
            info += $"\n획득: {acquisitionDate:yyyy-MM-dd HH:mm}";
            
            return info;
        }
        
        /// <summary>
        /// 네트워크 직렬화
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref itemId);
            serializer.SerializeValue(ref instanceId);
            serializer.SerializeValue(ref quantity);
            serializer.SerializeValue(ref currentDurability);
            serializer.SerializeValue(ref acquisitionTime);
            
            // string[] 직렬화 - Unity Netcode가 지원하지 않으므로 수동 처리
            if (serializer.IsReader)
            {
                int enchantmentCount = 0;
                serializer.SerializeValue(ref enchantmentCount);
                enchantments = new string[enchantmentCount];
                for (int i = 0; i < enchantmentCount; i++)
                {
                    serializer.SerializeValue(ref enchantments[i]);
                }
            }
            else
            {
                int enchantmentCount = enchantments?.Length ?? 0;
                serializer.SerializeValue(ref enchantmentCount);
                if (enchantments != null)
                {
                    for (int i = 0; i < enchantmentCount; i++)
                    {
                        serializer.SerializeValue(ref enchantments[i]);
                    }
                }
            }
            
            // Dictionary<string, string> 직렬화
            if (serializer.IsReader)
            {
                int customDataCount = 0;
                serializer.SerializeValue(ref customDataCount);
                customData = new Dictionary<string, string>();
                for (int i = 0; i < customDataCount; i++)
                {
                    string key = "";
                    string value = "";
                    serializer.SerializeValue(ref key);
                    serializer.SerializeValue(ref value);
                    customData[key] = value;
                }
            }
            else
            {
                int customDataCount = customData?.Count ?? 0;
                serializer.SerializeValue(ref customDataCount);
                if (customData != null)
                {
                    foreach (var kvp in customData)
                    {
                        string key = kvp.Key;
                        string value = kvp.Value;
                        serializer.SerializeValue(ref key);
                        serializer.SerializeValue(ref value);
                    }
                }
            }
        }
        
        /// <summary>
        /// 배열 비교 유틸리티
        /// </summary>
        private bool ArrayEquals(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        
        /// <summary>
        /// 디버그 정보
        /// </summary>
        public override string ToString()
        {
            return $"{ItemData?.ItemName ?? "Unknown"} x{quantity} ({instanceId[..8]})";
        }
    }
}