using UnityEngine;

public class EscapeMenuController : MonoBehaviour
{
    [Header("Menu Button References")]
    public TMP2DButton LeaveButton;
    public TMP2DButton optionButton;
    public TMP2DButton resumeButton;

    private MenuId targetMenu = MenuId.EscapeMenu;
    private MenuId hudMenu = MenuId.HUD;

    private void OnEnable()
    {
        if (LeaveButton != null)
            LeaveButton.onClick.AddListener(OnLeaveClick);

        if (optionButton != null)
            optionButton.onClick.AddListener(OnOptionClick);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClick);
    }

    private void OnDisable()
    {
        if (LeaveButton != null)
            LeaveButton.onClick.RemoveAllListeners();

        if (optionButton != null)
            optionButton.onClick.RemoveAllListeners();

        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
    }

    private void OnLeaveClick()
    {
        UIManager.Instance.Open(MenuId.LeaveConfirmDialog);
    }

    private void OnOptionClick()
    {
        UIManager.Instance.Open(MenuId.Settings);
    }

    private void OnResumeClick()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.Toggle(targetMenu);

        bool nowOpen = UIManager.Instance.IsOpen(targetMenu);

        var cameraController = FindObjectOfType<CameraController>();

        if (!nowOpen)
        {
            UIManager.Instance.Open(hudMenu);
            if (cameraController != null) cameraController.OnMatchClick();
        }
        else
        {
            if (cameraController != null) cameraController.ToFrontViewCam();
        }
    }
}