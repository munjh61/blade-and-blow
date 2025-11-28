using UnityEngine;

public class SelectMatchModeController : MonoBehaviour
{
    [Header("Menu Button References")]
    public TMP2DButton SingleMatchButton;
    public TMP2DButton TeamMatchButton;
    //public TMP2DButton PrivateMatchButton;
    public TMP2DButton exitButton;

    private void OnEnable()
    {
        if (SingleMatchButton != null)  SingleMatchButton.onClick.AddListener(OnSingleMatchClick);
        if (TeamMatchButton != null)    TeamMatchButton.onClick.AddListener(OnTeamMatchClick);
        //if (PrivateMatchButton != null) PrivateMatchButton.onClick.AddListener(OnPrivateMatchClick);
        if (exitButton != null)         exitButton.onClick.AddListener(BackToMainMenu);
        GameStateManager.OnRematchRequested += HandleRematchRequest;
    }

    private void OnDisable()
    {
        if (SingleMatchButton != null)  SingleMatchButton.onClick.RemoveAllListeners();
        if (TeamMatchButton != null)    TeamMatchButton.onClick.RemoveAllListeners();
        //if (PrivateMatchButton != null) PrivateMatchButton.onClick.RemoveAllListeners();
        if (exitButton != null)         exitButton.onClick.RemoveAllListeners();
        GameStateManager.OnRematchRequested -= HandleRematchRequest;
    }

    private void HandleRematchRequest()
    {
        GameStateManager gsm = GameStateManager.Instance;
        switch (gsm.currentMatchMode)
        {
            case MatchMode.SingleMatch:
                OnSingleMatchClick();
                break;

            case MatchMode.TeamMatch:
                OnTeamMatchClick();
                break;

            case MatchMode.PrivateMatch:
                if (!string.IsNullOrEmpty(gsm.roomCode))
                {
                    ApplyMatchMode(MatchMode.PrivateMatch);
                    EnterMatchingMode();
                    PhotonMatchingAgent.Instance.StartPrivate(gsm.roomCode);
                    UIManager.Instance.Open(MenuId.MatchInfoPanel, (MenuId.SelectWeapon, new SelectWeaponArgs { character = null, targetSlot = Slot.Weapon }), MenuId.CurrentSelectedWeapon);
                }
                else
                {
                    OnPrivateMatchClick();
                }
                break;
        }
    }

    private void ApplyMatchMode(MatchMode mode)
    {
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.SetMatchMode(mode);
        else
            Debug.LogWarning("[SelectMatchModeController] GameStateManager instance not found.");
    }

    public void EnterMatchingMode()
    {
        // CameraController 로직 호출
        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.OnMatchClick();
    }

    public void BackToMainMenu()
    {
        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.ToFrontViewCam();

        UIManager.Instance.Open(MenuId.MainMenu);
    }

    public void OnSingleMatchClick()
    {
        ApplyMatchMode(MatchMode.SingleMatch);
        EnterMatchingMode();
        PhotonMatchingAgent.Instance.StartSingle();
        UIManager.Instance.Open(MenuId.MatchInfoPanel, (MenuId.SelectWeapon, new SelectWeaponArgs { character = null, targetSlot = Slot.Weapon }), MenuId.CurrentSelectedWeapon);
    }

    public void OnTeamMatchClick()
    {
        ApplyMatchMode(MatchMode.TeamMatch);
        EnterMatchingMode();
        PhotonMatchingAgent.Instance.StartTeam();
        UIManager.Instance.Open(MenuId.MatchInfoPanel, (MenuId.SelectWeapon, new SelectWeaponArgs { character = null, targetSlot = Slot.Weapon }), MenuId.CurrentSelectedWeapon);
    }

    public void OnPrivateMatchClick()
    {
        ApplyMatchMode(MatchMode.PrivateMatch);
        UIManager.Instance.Open(MenuId.PrivateMatchMenus);
    }
}