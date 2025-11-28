using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;

    private IMatchInfoProvider _provider;
    private bool _isSubscribed;
    private Coroutine _bindLoop;

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.MatchInfoProviderChanged += OnProviderAvailable;

        AttachProviderOnce();
        _bindLoop = StartCoroutine(BindLoop());
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.MatchInfoProviderChanged -= OnProviderAvailable;

        if (_bindLoop != null) { StopCoroutine(_bindLoop); _bindLoop = null; }
        DetachProvider();
    }

    private void OnProviderAvailable(IMatchInfoProvider p)
    {
        if (_provider == p) return;
        DetachProvider();
        _provider = p;
        SubscribeAndRender();
    }

    private void AttachProviderOnce()
    {
        _provider = GameStateManager.Instance?.MatchInfoProvider;
        if (_provider != null && !_isSubscribed)
        {
            SubscribeAndRender();
        }
        else if (_provider == null)
        {
            RenderFallbackFromGsm();
        }
    }

    private System.Collections.IEnumerator BindLoop()
    {
        const float period = 0.2f;
        while (enabled && gameObject.activeInHierarchy)
        {
            var gsm = GameStateManager.Instance;

            // 파괴된 Unity Object 감지
            if (_provider is Object uo && uo == null)
            {
                DetachProvider();
                _provider = null;
            }

            var current = gsm?.MatchInfoProvider;
            if (!ReferenceEquals(current, _provider))
            {
                DetachProvider();
                _provider = current;
                SubscribeAndRender();
            }

            yield return new WaitForSeconds(period);
        }
    }

    private void SubscribeAndRender()
    {
        if (_provider != null && !_isSubscribed)
        {
            _provider.OnChanged += Render;
            _isSubscribed = true;
            Render(_provider.GetSnapshot());
        }
        else if (_provider == null)
        {
            RenderFallbackFromGsm();
        }
    }

    private void AttachProviderAndRender()
    {
        _provider = GameStateManager.Instance?.MatchInfoProvider;
        if (_provider != null && !_isSubscribed)
        {
            _provider.OnChanged += Render;
            _isSubscribed = true;
            Render(_provider.GetSnapshot());
        }
        else if (_provider == null)
        {
            RenderFallbackFromGsm();
        }
    }

    private void DetachProvider()
    {
        if (_provider != null && _isSubscribed)
            _provider.OnChanged -= Render;

        _isSubscribed = false;
        _provider = null;
    }

    private void Render(MatchInfoSnapshot s)
    {
        if (!timerText) return;
        int ss = Mathf.Max(0, s.Timer);
        timerText.text = $"{ss:00}";
    }

    private void RenderFallbackFromGsm()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        if (timerText != null)
            timerText.text = gsm.GetTimerDisplayText();
    }
}
