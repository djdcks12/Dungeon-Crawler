using UnityEngine;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 리소스 로더 - Resources 폴더의 스프라이트들을 로드하고 관리
    /// </summary>
    public static class ResourceLoader
    {
        // 캐싱된 스프라이트들
        private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
        private static Dictionary<string, Sprite[]> cachedSpriteSheets = new Dictionary<string, Sprite[]>();
        
        /// <summary>
        /// 플레이어 스프라이트 로드 (종족별)
        /// </summary>
        public static Sprite GetPlayerSprite(Race race, PlayerAnimationType animType = PlayerAnimationType.Idle, int direction = 0)
        {
            string spritePath = GetPlayerSpritePath(race, animType, direction);
            return LoadSprite(spritePath);
        }
        
        /// <summary>
        /// 몬스터 스프라이트 로드
        /// </summary>
        public static Sprite GetMonsterSprite(MonsterType monsterType, MonsterAnimationType animType = MonsterAnimationType.Idle)
        {
            string spritePath = GetMonsterSpritePath(monsterType, animType);
            return LoadSprite(spritePath);
        }
        
        /// <summary>
        /// 스프라이트 시트 로드 (애니메이션용)
        /// </summary>
        public static Sprite[] GetSpriteSheet(string path)
        {
            if (cachedSpriteSheets.ContainsKey(path))
            {
                return cachedSpriteSheets[path];
            }
            
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                cachedSpriteSheets[path] = sprites;
                return sprites;
            }
            
            Debug.LogWarning($"Sprite sheet not found: {path}");
            return new Sprite[0];
        }
        
        /// <summary>
        /// 단일 스프라이트 로드
        /// </summary>
        private static Sprite LoadSprite(string path)
        {
            if (cachedSprites.ContainsKey(path))
            {
                return cachedSprites[path];
            }
            
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                cachedSprites[path] = sprite;
                return sprite;
            }
            
            Debug.LogWarning($"Sprite not found: {path}");
            return null;
        }
        
        /// <summary>
        /// 플레이어 스프라이트 경로 생성 (종족별)
        /// </summary>
        private static string GetPlayerSpritePath(Race race, PlayerAnimationType animType, int direction)
        {
            string raceFolder = "Characters/Body_A"; // 모든 종족이 같은 몸체 사용
            string animFolder = GetPlayerAnimationFolder(animType);
            string directionName = GetDirectionName(direction);
            
            // 예: Characters/Body_A/Animations/Idle_Base/Idle_Down-Sheet
            return $"{raceFolder}/Animations/{animFolder}/{animFolder.Replace("_Base", "")}_{directionName}-Sheet";
        }
        
        /// <summary>
        /// 몬스터 스프라이트 경로 생성
        /// </summary>
        private static string GetMonsterSpritePath(MonsterType monsterType, MonsterAnimationType animType)
        {
            string monsterFolder = GetMonsterFolder(monsterType);
            string animName = animType.ToString();
            
            // 예: Entities/Mobs/Orc Crew/Orc/Idle/Idle-Sheet
            return $"Entities/Mobs/{monsterFolder}/{animName}/{animName}-Sheet";
        }
        
        /// <summary>
        /// 플레이어 애니메이션 폴더명 가져오기
        /// </summary>
        private static string GetPlayerAnimationFolder(PlayerAnimationType animType)
        {
            switch (animType)
            {
                case PlayerAnimationType.Idle: return "Idle_Base";
                case PlayerAnimationType.Walk: return "Walk_Base";
                case PlayerAnimationType.Run: return "Run_Base";
                case PlayerAnimationType.Attack_Slice: return "Slice_Base";
                case PlayerAnimationType.Attack_Pierce: return "Pierce_Base";
                case PlayerAnimationType.Hit: return "Hit_Base";
                case PlayerAnimationType.Death: return "Death_Base";
                case PlayerAnimationType.Collect: return "Collect_Base";
                default: return "Idle_Base";
            }
        }
        
        /// <summary>
        /// 몬스터 폴더명 가져오기
        /// </summary>
        private static string GetMonsterFolder(MonsterType monsterType)
        {
            switch (monsterType)
            {
                case MonsterType.Orc: return "Orc Crew/Orc";
                case MonsterType.OrcWarrior: return "Orc Crew/Orc - Warrior";
                case MonsterType.OrcRogue: return "Orc Crew/Orc - Rogue";
                case MonsterType.OrcShaman: return "Orc Crew/Orc - Shaman";
                case MonsterType.Skeleton: return "Skeleton Crew/Skeleton - Base";
                case MonsterType.SkeletonWarrior: return "Skeleton Crew/Skeleton - Warrior";
                case MonsterType.SkeletonRogue: return "Skeleton Crew/Skeleton - Rogue";
                case MonsterType.SkeletonMage: return "Skeleton Crew/Skeleton - Mage";
                default: return "Orc Crew/Orc";
            }
        }
        
        /// <summary>
        /// 방향명 가져오기
        /// </summary>
        private static string GetDirectionName(int direction)
        {
            switch (direction)
            {
                case 0: return "Down";  // 아래
                case 1: return "Side";  // 옆
                case 2: return "Up";    // 위
                default: return "Down";
            }
        }
        
        /// <summary>
        /// 무기 스프라이트 로드
        /// </summary>
        public static Sprite GetWeaponSprite(WeaponType weaponType)
        {
            string weaponPath = GetWeaponSpritePath(weaponType);
            return LoadSprite(weaponPath);
        }
        
        /// <summary>
        /// 무기 스프라이트 경로 생성
        /// </summary>
        private static string GetWeaponSpritePath(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Longsword:
                case WeaponType.Rapier:
                case WeaponType.Broadsword:
                    return "Weapons/Wood/Wood";
                    
                case WeaponType.Mace:
                case WeaponType.Warhammer:
                    return "Weapons/Bone/Bone";
                    
                case WeaponType.Dagger:
                case WeaponType.CurvedDagger:
                    return "Weapons/Bone/Bone";
                    
                case WeaponType.Fists:
                    return "Weapons/Hands/Hands";
                    
                default:
                    return "Weapons/Wood/Wood";
            }
        }
        
        /// <summary>
        /// 캐시 클리어
        /// </summary>
        public static void ClearCache()
        {
            cachedSprites.Clear();
            cachedSpriteSheets.Clear();
            Debug.Log("ResourceLoader cache cleared");
        }
        
        /// <summary>
        /// 환경 타일 스프라이트 로드
        /// </summary>
        public static Sprite GetEnvironmentSprite(string spriteName)
        {
            string path = $"Environment/Tilesets/{spriteName}";
            return LoadSprite(path);
        }
        
        /// <summary>
        /// 아이템 스프라이트 로드 (추후 아이템 시스템에서 사용)
        /// </summary>
        public static Sprite GetItemSprite(string itemName)
        {
            string path = $"Environment/Props/Static/{itemName}";
            return LoadSprite(path);
        }
    }
    
    /// <summary>
    /// 플레이어 애니메이션 타입
    /// </summary>
    public enum PlayerAnimationType
    {
        Idle,
        Walk,
        Run,
        Attack_Slice,
        Attack_Pierce,
        Hit,
        Death,
        Collect,
        Carry_Idle,
        Carry_Walk,
        Carry_Run
    }
    
    /// <summary>
    /// 몬스터 타입
    /// </summary>
    public enum MonsterType
    {
        Orc,
        OrcWarrior,
        OrcRogue,
        OrcShaman,
        Skeleton,
        SkeletonWarrior,
        SkeletonRogue,
        SkeletonMage
    }
    
    /// <summary>
    /// 몬스터 애니메이션 타입
    /// </summary>
    public enum MonsterAnimationType
    {
        Idle,
        Run,
        Death
    }
}