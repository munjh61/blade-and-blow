using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Play
{
    public class KillFeedController : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private RectTransform content;
        [SerializeField] private KillFeedEntry entryPrefab;
        [SerializeField] private Transform poolRoot;

        [Header("Capacity")]
        [Tooltip("동시 노출 가능한 최대 줄 수 (넘치면 가장 오래된 것부터 즉시 제거)")]
        public int maxVisible = 3;

        public Sprite[] weaponIcons;

        bool _ready;
        readonly Queue<KillEvent> _pending = new();
        readonly List<KillFeedEntry> _active = new();
        readonly Stack<KillFeedEntry> _pool = new();

        void OnEnable() { StartCoroutine(BootstrapLayout()); }
        void OnDisable() { Clear(); }

        IEnumerator BootstrapLayout()
        {
            _ready = false;
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            _ready = true;

            // 대기중이던 이벤트 처리
            while (_pending.Count > 0) Spawn(_pending.Dequeue());
        }

        public void Show(KillEvent e)
        {
            if (!_ready) { _pending.Enqueue(e); return; }
            Spawn(e);
        }

        void Spawn(KillEvent e)
        {
            _active.RemoveAll(x => x == null);

            if (_active.Count >= maxVisible)
                Recycle(_active[0]);

            // 풀에서 가져오기
            var go = GetFromPool();
            go.transform.SetParent(content, false);

            if (go.TryGetComponent<LayoutElement>(out var leRoot))
                leRoot.ignoreLayout = false;

            // 데이터 바인딩
            var row = go.GetComponent<KillFeedEntry>();
            row.Set(e);

            // 레이아웃 즉시 갱신 후 노출
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            go.SetActive(true);

            StartCoroutine(row.PlayRoutine(() => Recycle(row)));
            _active.Add(row);
        }

        GameObject GetFromPool()
        {
            KillFeedEntry inst;
            if (_pool.Count > 0) inst = _pool.Pop();
            else
            {
                inst = Instantiate(entryPrefab, poolRoot);
                inst.gameObject.SetActive(false);
            }

            return inst.gameObject;
        }

        void Recycle(KillFeedEntry row)
        {
            if (!row) return;

            _active.Remove(row);
            row.gameObject.SetActive(false);
            if (row.TryGetComponent<LayoutElement>(out var le)) le.ignoreLayout = true;
            row.transform.SetParent(poolRoot, false);
            _pool.Push(row);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                Recycle(_active[i]);
            _active.Clear();
        }
    }
}