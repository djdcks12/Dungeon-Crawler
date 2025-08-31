using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 장비 슬롯 열거형 - 착용 가능한 장비 위치 정의
    /// </summary>
    public enum EquipmentSlot
    {
        None,           // 없음
        Head,           // 머리 (투구, 모자)
        Chest,          // 가슴 (갑옷, 상의)
        Legs,           // 다리 (하의, 바지)
        Feet,           // 발 (신발, 부츠)
        Hands,          // 손 (장갑)
        Belt,           // 허리 (벨트, 허리띠)
        MainHand,       // 주무기 (검, 둔기, 단검, 지팡이)
        OffHand,        // 보조무기/방패
        TwoHand,        // 양손무기 (활, 대형무기)
        Ring1,          // 반지1
        Ring2,          // 반지2
        Necklace,       // 목걸이
        Earring1,       // 귀걸이1
        Earring2        // 귀걸이2
    }
    
    /// <summary>
    /// 장비 저장 데이터
    /// </summary>
    [System.Serializable]
    public class EquipmentSaveData
    {
        public Dictionary<EquipmentSlot, ItemInstance> equippedItems;
    }
    
    /// <summary>
    /// 네트워크 직렬화용 장비 슬롯 데이터
    /// </summary>
    [System.Serializable]
    public struct EquipmentSlotData : INetworkSerializable
    {
        public EquipmentSlot slot;
        public ItemInstance item;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref slot);
            serializer.SerializeValue(ref item);
        }
    }
}