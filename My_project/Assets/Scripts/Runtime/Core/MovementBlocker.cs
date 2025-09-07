using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 막힌 방향 감지 및 이동 필터링 시스템
    /// 애니메이션은 유지하면서 실제 이동만 제한
    /// </summary>
    public static class MovementBlocker
    {
        // Layer 설정 (현재 DungeonPlayer가 Layer 3에 있음)
        public static readonly int EntityLayer = 3;
        public static LayerMask EntityMask = (1 << EntityLayer);
        
        // 충돌 설정
        public static float BlockRadius = 0.5f;        // 충돌 감지 반경
        public static float MinDistance = 0.7f;        // 최소 유지 거리
        public static float CheckDistance = 0.3f;      // 전방 체크 거리
        
        /// <summary>
        /// 막힌 방향을 필터링해서 실제 이동 가능한 벡터 반환
        /// </summary>
        /// <param name="mover">이동하려는 Transform</param>
        /// <param name="desiredMovement">원하는 이동 벡터</param>
        /// <returns>실제 이동 가능한 벡터</returns>
        public static Vector2 FilterMovement(Transform mover, Vector2 desiredMovement)
        {
            if (mover == null || desiredMovement.magnitude < 0.01f)
                return desiredMovement;
            
            Vector3 currentPos = mover.position;
            Vector2 filteredMovement = Vector2.zero;
            
            // X축 이동 체크
            if (Mathf.Abs(desiredMovement.x) > 0.01f)
            {
                if (CanMoveInDirection(currentPos, Vector2.right * Mathf.Sign(desiredMovement.x), mover))
                {
                    filteredMovement.x = desiredMovement.x;
                }
            }
            
            // Y축 이동 체크
            if (Mathf.Abs(desiredMovement.y) > 0.01f)
            {
                if (CanMoveInDirection(currentPos, Vector2.up * Mathf.Sign(desiredMovement.y), mover))
                {
                    filteredMovement.y = desiredMovement.y;
                }
            }
            
            return filteredMovement;
        }
        
        /// <summary>
        /// 특정 방향으로 이동 가능한지 체크 (접근하는 방향만 차단)
        /// </summary>
        private static bool CanMoveInDirection(Vector3 position, Vector2 direction, Transform ignore)
        {
            // 현재 위치 근처의 모든 엔티티 확인
            Collider2D[] nearbyHits = Physics2D.OverlapCircleAll(position, MinDistance, EntityMask);
            
            // 자기 자신을 제외한 다른 엔티티들 체크
            foreach (var hit in nearbyHits)
            {
                if (hit.transform == ignore) continue; // 자기 자신 제외
                
                Vector3 toOther = hit.transform.position - position;
                float currentDistance = toOther.magnitude;
                
                // 이동 후 해당 엔티티와의 거리 계산
                Vector3 afterMovePos = position + (Vector3)direction * CheckDistance;
                float afterMoveDistance = Vector3.Distance(afterMovePos, hit.transform.position);
                
                // 이동 후 더 가까워지면서 최소 거리보다 가까우면 차단
                if (afterMoveDistance < currentDistance && afterMoveDistance < MinDistance)
                {
                    return false;
                }
            }
            
            // 추가로 이동 방향 전방 체크
            Vector3 checkPosition = position + (Vector3)direction * CheckDistance;
            Collider2D[] frontHits = Physics2D.OverlapCircleAll(checkPosition, BlockRadius, EntityMask);
            
            foreach (var hit in frontHits)
            {
                if (hit.transform == ignore) continue;
                
                // 전방에 다른 엔티티가 있으면 차단
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 특정 위치 주변에 다른 엔티티가 있는지 체크
        /// </summary>
        /// <param name="position">체크할 위치</param>
        /// <param name="ignore">무시할 Transform</param>
        /// <returns>다른 엔티티가 있으면 true</returns>
        public static bool IsBlocked(Vector3 position, Transform ignore = null)
        {
            Collider2D hit = Physics2D.OverlapCircle(position, BlockRadius, EntityMask);
            return hit != null && hit.transform != ignore;
        }
        
        /// <summary>
        /// 가장 가까운 다른 엔티티까지의 거리 반환
        /// </summary>
        /// <param name="position">기준 위치</param>
        /// <param name="ignore">무시할 Transform</param>
        /// <returns>가장 가까운 거리, 없으면 float.MaxValue</returns>
        public static float GetNearestEntityDistance(Vector3 position, Transform ignore = null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, MinDistance * 2f, EntityMask);
            float nearestDistance = float.MaxValue;
            
            foreach (var hit in hits)
            {
                if (hit.transform == ignore) continue;
                
                float distance = Vector2.Distance(position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }
            
            return nearestDistance;
        }
        
    }
}