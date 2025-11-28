using UnityEngine;

public class LeaveConfirmController : MonoBehaviour
{
    [Header("ESC로 토글할 메뉴")]
    public MenuId targetMenu = MenuId.LeaveConfirmDialog;
    public MenuId escapeMenu = MenuId.EscapeMenu;

    [Header("Menu Button References")]
    public TMP2DButton cancelButton;
    public TMP2DButton LeaveButton;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) OnCancelClick();
    }    

    private void OnEnable()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);

        if (LeaveButton != null)
            LeaveButton.onClick.AddListener(OnLeaveClick);
    }

    private void OnDisable()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();

        if (LeaveButton != null)
            LeaveButton.onClick.RemoveAllListeners();
    }

    private void OnLeaveClick()
    {
        Debug.Log("[LeaveConfirmController] Leave button clicked. Using GameExitManager...");

        if (GameExitManager.Instance != null)
        {
            GameExitManager.Instance.LeaveGame();
        }
        else
        {
            Debug.LogError("[LeaveConfirmController] GameExitManager instance not found!");
        }
    }

    private void OnCancelClick()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.Toggle(targetMenu);

        bool nowOpen = UIManager.Instance.IsOpen(targetMenu);
        if (!nowOpen) UIManager.Instance.Open(escapeMenu);
    }
}