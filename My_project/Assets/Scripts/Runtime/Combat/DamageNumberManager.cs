using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// 데미지 넘버 매니저 - 오브젝트 풀링으로 데미지 텍스트 관리
    /// 씬에 하나만 존재하는 싱글톤
    /// </summary>
    public class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 30;

        private List<FloatingDamageNumber> pool;
        private int nextIndex = 0;
        private Canvas worldCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePool();
        }

        private void InitializePool()
        {
            // 월드 스페이스 캔버스 생성
            var canvasObj = new GameObject("DamageNumberCanvas");
            canvasObj.transform.SetParent(transform);
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            pool = new List<FloatingDamageNumber>(poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                var obj = CreateDamageNumber();
                obj.gameObject.SetActive(false);
                pool.Add(obj);
            }
        }

        private FloatingDamageNumber CreateDamageNumber()
        {
            var go = new GameObject("DmgNum");
            go.transform.SetParent(worldCanvas.transform);
            go.transform.localScale = Vector3.one * 0.02f; // 월드 스페이스 크기 조정

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;

            // 아웃라인 추가 (가독성)
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            var dmgNum = go.AddComponent<FloatingDamageNumber>();

            // FloatingDamageNumber의 damageText 필드 설정 (SerializeField이므로 reflection 필요 없이 public 접근)
            // 대신 GetComponent로 접근
            return dmgNum;
        }

        /// <summary>
        /// 데미지 넘버 표시
        /// </summary>
        public void ShowDamage(Vector3 worldPosition, float damage, bool isCritical, DamageType damageType)
        {
            var dmgNum = GetFromPool();
            dmgNum.Show(worldPosition, damage, isCritical, damageType, false);
        }

        /// <summary>
        /// 힐 넘버 표시
        /// </summary>
        public void ShowHeal(Vector3 worldPosition, float healAmount)
        {
            var dmgNum = GetFromPool();
            dmgNum.Show(worldPosition, healAmount, false, DamageType.Holy, true);
        }

        /// <summary>
        /// 경험치 획득 표시
        /// </summary>
        public void ShowExp(Vector3 worldPosition, long exp)
        {
            var dmgNum = GetFromPool();
            dmgNum.Show(worldPosition + Vector3.up * 0.3f, exp, false, DamageType.Holy, false);
        }

        private FloatingDamageNumber GetFromPool()
        {
            var dmgNum = pool[nextIndex];
            nextIndex = (nextIndex + 1) % poolSize;
            return dmgNum;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
