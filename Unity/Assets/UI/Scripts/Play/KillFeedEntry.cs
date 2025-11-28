using Game.Domain;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Play
{
    public class KillFeedEntry : MonoBehaviour
    {
        [Header("Refs (Row 자식들)")]
        [SerializeField] private TextMeshProUGUI killerTMP;
        [SerializeField] private TextMeshProUGUI divTMP;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TextMeshProUGUI victimTMP;

        [Header("Anim")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeIn = 0.12f;
        [SerializeField] private float hold = 4.0f;
        [SerializeField] private float fadeOut = 0.25f;
        [SerializeField] private float popScale = 1.06f;
        [SerializeField] private float popDur = 0.10f;

        void Reset()
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void Set(KillEvent e)
        {
            if (killerTMP)
            {
                killerTMP.text = Safe(e.killerName);
                killerTMP.color = GetColor(e.killerTeam);
                killerTMP.fontWeight = e.teamKill ? FontWeight.Bold : FontWeight.Regular;
            }
            if (victimTMP)
            {
                victimTMP.text = Safe(e.victimName);
                victimTMP.color = GetColor(e.victimTeam);
                victimTMP.fontWeight = e.teamKill ? FontWeight.Bold : FontWeight.Regular;
            }

            bool useIcon = e.weaponIcon != null;
            if (weaponIcon) { weaponIcon.enabled = useIcon; weaponIcon.sprite = e.weaponIcon; }
        }
        
        private void OnDisable()
        {
            // 혹시라도 내부에서 돌린 코루틴이 있다면 정리
            StopAllCoroutines();
            if (group) group.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        public IEnumerator PlayRoutine(Action onFinished)
        {
            if (!group) group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            // 초기 상태
            group.alpha = 0f;
            transform.localScale = Vector3.one;

            // 팝 + 페이드인
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeIn);
                group.alpha = k;
                // 약한 팝
                float s = Mathf.SmoothStep(1f, popScale, Mathf.Clamp01(t / popDur));
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            group.alpha = 1f;
            transform.localScale = Vector3.one;

            // 유지
            float h = 0f;
            while (h < hold)
            {
                if (!this || !gameObject.activeInHierarchy) yield break;
                h += Time.unscaledDeltaTime;
                yield return null;
            }

            // 페이드아웃
            t = 0f;
            while (t < fadeOut)
            {
                if (!this || !gameObject.activeInHierarchy) yield break;
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / fadeOut);
                group.alpha = k;
                yield return null;
            }
            group.alpha = 0f;

            onFinished?.Invoke();
        }

        static string Safe(string s) => string.IsNullOrEmpty(s) ? "?" : s;

        private Color GetColor(TeamId team)
        {
            return team switch
            {
                TeamId.Red => new Color(1f, 0.4f, 0.4f),
                TeamId.Blue => new Color(0.4f, 0.6f, 1f),
                _ => Color.white,
            };
        }
    }
}