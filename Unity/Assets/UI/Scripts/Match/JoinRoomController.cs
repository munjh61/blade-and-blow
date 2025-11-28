using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomController : MonoBehaviour
{
    [Header("Menu Button References")]
    public InputField RoomCodeInputField;
    public TMP2DButton JoinButton;
    public TMP2DButton BackButton;
    public TMP_Text HelperOrErrorText;

    private void Awake()
    {
        EnsureCanvasScaler();

        if (RoomCodeInputField != null)
        {
            RoomCodeInputField.contentType = InputField.ContentType.Alphanumeric;
            RoomCodeInputField.characterLimit = 8;
        }
    }

    private void EnsureCanvasScaler()
    {
        // Canvas 찾기 (자신 또는 부모에서)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("Canvas를 찾을 수 없습니다!");
            return;
        }

        // Canvas Scaler가 이미 있는지 확인
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            // Canvas Scaler 추가 및 설정
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Debug.Log("Canvas Scaler 자동 추가 완료!");
        }
    }

    private void OnEnable()
    {
        if (JoinButton != null)         JoinButton.onClick.AddListener(OnJoinlick);
        if (BackButton != null)         BackButton.onClick.AddListener(BackToPrivateMatchButton);
        if (RoomCodeInputField != null) RoomCodeInputField.onValueChanged.AddListener(OnRoomCodeChanged);

        PhotonMatchingAgent.Instance.OnPrivateJoinFailed += HandlePrivateJoinFailed;
    }

    private void OnDisable()
    {
        if (JoinButton != null)         JoinButton.onClick.RemoveAllListeners();
        if (BackButton != null)         BackButton.onClick.RemoveAllListeners();
        if (RoomCodeInputField != null) RoomCodeInputField.onValueChanged.RemoveAllListeners();

        PhotonMatchingAgent.Instance.OnPrivateJoinFailed -= HandlePrivateJoinFailed;
    }

    private void OnRoomCodeChanged(string raw)
    {
        string normalized = NormalizeCode(raw);
        if (normalized != raw) RoomCodeInputField.SetTextWithoutNotify(normalized);
    }

    private void ValidateAndToggleJoin(string code)
    {
        bool ok = IsValidCode(code);
        if (JoinButton != null) JoinButton.interactable = ok;

        if (HelperOrErrorText != null)
        {
            if (string.IsNullOrEmpty(code))
                HelperOrErrorText.text = "Enter the 8-digit room code.";
            else if (!ok)
                HelperOrErrorText.text = "Please verify the 8-digit alphanumeric code.";
            else
                HelperOrErrorText.text = "";
        }
    }

    private string NormalizeCode(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string alnum = Regex.Replace(s, "[^A-Za-z0-9]", "");
        return alnum.ToUpperInvariant();
    }

    private bool IsValidCode(string code)
    {
        return Regex.IsMatch(code ?? "", "^[A-Z0-9]{8}$");
    }

    public void OnJoinlick()
    {
        string roomId = RoomCodeInputField?.text;
        roomId = NormalizeCode(roomId);

        if (!IsValidCode(roomId))
        {
            ValidateAndToggleJoin(roomId);
            return;
        }

        SetUiEnabled(false, "Joining the room...");
        PhotonMatchingAgent.Instance.StartPrivate(roomId);
        // TODO: 로비 전환 메뉴 구현되면 활성화
        //UIManager.Instance.Open(MenuId.Lobby);
    }

    public void BackToPrivateMatchButton()
    {
        UIManager.Instance.Open(MenuId.PrivateMatchMenus);
    }

    // =========================
    // 이벤트 핸들러 & 헬퍼
    // =========================

    private void HandlePrivateJoinFailed(short code, string message, string attemptedRoomId)
    {
        Debug.Log("[JoinRoomController] HandlePrivateJoinFailed");
        var roomId = ExtractCodeForDisplay(attemptedRoomId);

        SetUiEnabled(true, MapJoinFailMessage(code, roomId, message));
        if (RoomCodeInputField != null) RoomCodeInputField.ActivateInputField();
    }

    private void SetUiEnabled(bool enabled, string helperMsg = null)
    {
        if (RoomCodeInputField != null) RoomCodeInputField.interactable             = enabled;
        if (JoinButton != null) JoinButton.interactable                             = enabled;
        if (helperMsg != null && HelperOrErrorText != null) HelperOrErrorText.text  = helperMsg;
    }

    private static string ExtractCodeForDisplay(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return "";

        var m = Regex.Match(roomName, @"^(?:private_|public_)?([A-Za-z0-9]{8})$", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.ToUpperInvariant();

        var m2 = Regex.Match(roomName, @"([A-Za-z0-9]{8})");
        return m2.Success ? m2.Groups[1].Value.ToUpperInvariant() : roomName;
    }

    // Photon JoinRoom 실패 코드 → 메시지 매핑
    private string MapJoinFailMessage(short code, string roomId, string rawMsg)
    {
        // 32758: GameDoesNotExist, 32764: GameClosed, 32765: GameFull
        switch (code)
        {
            case 32758: return $"Room \"{roomId}\" was not found. Please check the code.";
            case 32764: return $"Room \"{roomId}\" is closed.";
            case 32765: return $"Room \"{roomId}\" is full.";
            default: return $"Failed to join the room (code {code}). Please check the code and try again.";
        }
    }

}
