using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 이펙트 관리자 - 타격, 투사체, 소환 이펙트 통합 관리
    /// </summary>
    public class EffectManager : NetworkBehaviour
    {
        public static EffectManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 타격 이펙트 재생 (타겟 위치에)
        /// </summary>
        public void PlayHitEffect(string effectDataName, Vector3 position, Transform target = null)
        {
            // Resources에서 EffectData 로드
            var effectData = Resources.Load<EffectData>($"ScriptableObjects/EffectData/{effectDataName}");
            if (effectData == null) return;
            
            GameObject effectObject = null;
            // Texture2D 배열이 있으면 풀에서 오브젝트 가져와서 재생
            if (effectData.HitEffectFrames != null && effectData.HitEffectFrames.Length > 0 && EffectObjectPool.Instance != null)
            {
                effectObject = EffectObjectPool.Instance.PlayTextureEffect(position, Quaternion.identity, effectData.HitEffectFrames, effectData.HitFrameRate, false, effectData.HitDuration, target);
            }
        }
        
        /// <summary>
        /// 투사체 스킬 이펙트 시작
        /// </summary>
        public void StartProjectileSkillEffect(EffectData effectData, Vector3 startPos, Vector3 targetPos, float range, System.Action<Vector3> onHitCallback = null)
        {
            if (!IsServer) return;
            
            StartCoroutine(ProjectileEffectCoroutine(effectData, startPos, targetPos, range, onHitCallback));
        }
        
        private IEnumerator ProjectileEffectCoroutine(EffectData effectData, Vector3 startPos, Vector3 targetPos, float range, System.Action<Vector3> onHitCallback)
        {
            if (effectData == null) yield break;
            
            Vector3 direction = (targetPos - startPos).normalized;
            Vector3 finalPos = startPos + direction * range;
            
            // Start 이펙트
            if (effectData.ProjectileStartFrames != null && effectData.ProjectileStartFrames.Length > 0)
            {
                PlayProjectilePhaseClientRpc(effectData.name, ProjectilePhase.Start, startPos, finalPos);
                yield return new WaitForSeconds(effectData.StartDuration);
            }
            
            // 투사체 날아가는 동안 Repeatable 이펙트
            float journeyTime = Vector3.Distance(startPos, finalPos) / Mathf.Max(0.1f, effectData.ProjectileSpeed);
            float elapsedTime = 0f;
            Vector3 currentPos = startPos;
            
            while (elapsedTime < journeyTime)
            {
                elapsedTime += Time.deltaTime;
                currentPos = Vector3.Lerp(startPos, finalPos, elapsedTime / journeyTime);
                
                // 몬스터와 충돌 체크
                var hitMonster = Physics2D.OverlapCircle(currentPos, 0.5f, LayerMask.GetMask("Monster"));
                if (hitMonster != null)
                {
                    finalPos = currentPos;
                    break;
                }
                
                // Repeatable 이펙트 (옵션)
                if (effectData.ProjectileRepeatableFrames != null && effectData.ProjectileRepeatableFrames.Length > 0)
                {
                    PlayProjectilePhaseClientRpc(effectData.name, ProjectilePhase.Repeatable, currentPos, finalPos);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // Hit 이펙트
            if (effectData.ProjectileHitFrames != null && effectData.ProjectileHitFrames.Length > 0)
            {
                PlayProjectilePhaseClientRpc(effectData.name, ProjectilePhase.Hit, finalPos, finalPos);
            }
            
            // 콜백 호출
            onHitCallback?.Invoke(finalPos);
        }
        
        [ClientRpc]
        private void PlayProjectilePhaseClientRpc(string effectDataName, ProjectilePhase phase, Vector3 position, Vector3 targetPos)
        {
            var effectData = Resources.Load<EffectData>($"Effects/{effectDataName}");
            if (effectData == null) return;
            
            // 방향 계산
            Vector3 direction = (targetPos - position).normalized;
            Quaternion rotation = direction != Vector3.zero ? Quaternion.LookRotation(Vector3.forward, direction) : Quaternion.identity;
            
            GameObject effectObject = null;

            // Sprite 배열 가져오기
            Sprite[] frames = null;
            float frameRate = effectData.ProjectileFrameRate;
            
            switch (phase)
            {
                case ProjectilePhase.Start:
                    frames = effectData.ProjectileStartFrames;
                    break;
                case ProjectilePhase.Repeatable:
                    frames = effectData.ProjectileRepeatableFrames;
                    break;
                case ProjectilePhase.Hit:
                    frames = effectData.ProjectileHitFrames;
                    break;
            }
            
            float duration = phase switch
            {
                ProjectilePhase.Start => effectData.StartDuration,
                ProjectilePhase.Repeatable => 0.2f,
                ProjectilePhase.Hit => effectData.HitEffectDuration,
                _ => 1f
            };
            
            // Texture2D 배열이 있으면 풀에서 오브젝트 가져와서 재생
            if (frames != null && frames.Length > 0 && EffectObjectPool.Instance != null)
            {
                bool loop = phase == ProjectilePhase.Repeatable;
                effectObject = EffectObjectPool.Instance.PlayTextureEffect(position, rotation, frames, frameRate, loop, duration);
            }
        }
        
        /// <summary>
        /// 소환 스킬 이펙트 시작
        /// </summary>
        public void StartSummonSkillEffect(EffectData effectData, Vector3 position, System.Action<Vector3, float> onDamageCallback = null)
        {
            if (!IsServer) return;
            
            StartCoroutine(SummonEffectCoroutine(effectData, position, onDamageCallback));
        }
        
        private IEnumerator SummonEffectCoroutine(EffectData effectData, Vector3 position, System.Action<Vector3, float> onDamageCallback)
        {
            if (effectData == null) yield break;
            
            // Start 단계
            PlaySummonPhaseClientRpc(effectData.name, SummonPhase.Start, position);
            yield return new WaitForSeconds(effectData.SummonStartDuration);
            
            // Active 단계 (데미지 적용)
            PlaySummonPhaseClientRpc(effectData.name, SummonPhase.Active, position);
            float activeTime = 0f;
            float damageTime = 0f;
            
            while (activeTime < effectData.SummonActiveDuration)
            {
                activeTime += Time.deltaTime;
                damageTime += Time.deltaTime;
                
                // 데미지 간격마다 적용
                if (damageTime >= effectData.DamageInterval)
                {
                    onDamageCallback?.Invoke(position, effectData.DamageRadius);
                    damageTime = 0f;
                }
                
                // Repeatable 단계 (옵션)
                if (effectData.SummonRepeatableFrames != null && effectData.SummonRepeatableFrames.Length > 0 && activeTime % effectData.SummonRepeatableDuration < Time.deltaTime)
                {
                    PlaySummonPhaseClientRpc(effectData.name, SummonPhase.Repeatable, position);
                }
                
                yield return null;
            }
            
            // Ending 단계
            PlaySummonPhaseClientRpc(effectData.name, SummonPhase.Ending, position);
            yield return new WaitForSeconds(effectData.SummonEndingDuration);
        }
        
        [ClientRpc]
        private void PlaySummonPhaseClientRpc(string effectDataName, SummonPhase phase, Vector3 position)
        {
            var effectData = Resources.Load<EffectData>($"Effects/{effectDataName}");
            if (effectData == null) return;
            
            GameObject effectObject = null;
            
            // Sprite 배열 가져오기
            Sprite[] frames = null;
            float frameRate = effectData.SummonFrameRate;
            
            switch (phase)
            {
                case SummonPhase.Start:
                    frames = effectData.SummonStartFrames;
                    break;
                case SummonPhase.Active:
                    frames = effectData.SummonActiveFrames;
                    break;
                case SummonPhase.Repeatable:
                    frames = effectData.SummonRepeatableFrames;
                    break;
                case SummonPhase.Ending:
                    frames = effectData.SummonEndingFrames;
                    break;
            }
            
            float duration = phase switch
            {
                SummonPhase.Start => effectData.SummonStartDuration,
                SummonPhase.Active => effectData.SummonActiveDuration,
                SummonPhase.Repeatable => effectData.SummonRepeatableDuration,
                SummonPhase.Ending => effectData.SummonEndingDuration,
                _ => 1f
            };
            
            // Texture2D 배열이 있으면 풀에서 오브젝트 가져와서 재생
            if (frames != null && frames.Length > 0 && EffectObjectPool.Instance != null)
            {
                bool loop = phase == SummonPhase.Repeatable;
                effectObject = EffectObjectPool.Instance.PlayTextureEffect(position, Quaternion.identity, frames, frameRate, loop, duration);
            }
        }
    }
}