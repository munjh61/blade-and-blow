using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class MatchInfoController : MonoBehaviour
{
    [Header("Panel Button References")]
    public TMP2DButton backButton;

    [Header("UI References")]
    public TextMeshProUGUI matchModeText;
    public TextMeshProUGUI playersCountText;
    public Text statusText;

    [Header("Private Room UI (optional)")]
    public GameObject privateOnlyPanel;
    public TextMeshProUGUI roomCodeText;

    private IMatchInfoProvider _provider;
    private bool _isSubscribed;
    private Coroutine _bindLoop;

    private void OnEnable()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged += OnProviderAvailable;

        AttachProviderOnce();
        _bindLoop = StartCoroutine(BindLoop());
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.MatchInfoProviderChanged -= OnProviderAvailable;

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
            SubscribeAndRender();
        else if (_provider == null)
            RenderFallbackFromGsm();
    }

    private IEnumerator BindLoop()
    {
        const float period = 0.2f;
        while (enabled && gameObject.activeInHierarchy)
        {
            var gsm = GameStateManager.Instance;

            // 파괴된 Unity Object 방어
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
                if (_provider != null) SubscribeAndRender();
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
        if (matchModeText != null) matchModeText.text = s.Mode;
        if (playersCountText != null) playersCountText.text = $"{s.CurrentPlayers} / {s.MaxPlayers} Players";
        if (statusText != null) statusText.text = s.StatusText ?? "";

        if (privateOnlyPanel != null) privateOnlyPanel.SetActive(s.IsPrivate);
        if (roomCodeText != null)
            roomCodeText.text = s.IsPrivate
                ? (string.IsNullOrEmpty(s.RoomCode) ? "Creating Room..." : $"Room: {s.RoomCode}")
                : "";
    }

    private void RenderFallbackFromGsm()
    {
        var gsm = GameStateManager.Instance;
        if (gsm == null) return;

        if (matchModeText != null)
            matchModeText.text = gsm.GetMatchModeDisplayText();

        if (playersCountText != null)
            playersCountText.text = $"{gsm.maxPlayers} Players";

        // 상태/프라이빗은 알 수 없으니 초기화 정도만
        if (statusText != null)
            statusText.text = "";

        if (privateOnlyPanel != null)
            privateOnlyPanel.SetActive(false);

        if (roomCodeText != null)
            roomCodeText.text = "";
    }

    public void SetMatchingStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void OnBackClick()
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null) cameraController.ToFrontViewCam();

        PhotonMatchingAgent.Instance.Cancel();
        UIManager.Instance.Open(MenuId.SelectMode);
    }
}