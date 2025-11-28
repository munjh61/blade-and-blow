using UnityEngine;

public class EscapeKeyController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnEscapePress();
        }
    }

    private void OnEscapePress()
    {
        if (UIManager.Instance == null) return;        

        var cameraController = FindObjectOfType<CameraController>();
        if (!cameraController.IsPlayScene()) return;

        if(UIManager.Instance.GetOpenMenus().Count > 0)
        {
            MenuId nowOpenMenu = UIManager.Instance.GetOpenMenus()[0];
            switch (nowOpenMenu)
            {
                case MenuId.HUD: //HUD -> Escape Menu 열기
                    UIManager.Instance.Open(MenuId.EscapeMenu);
                    if (cameraController != null) cameraController.ToFrontViewCam();
                    break;

                case MenuId.LeaveConfirmDialog: //Leave Confirm -> Leave Confirm 닫기 == Escape Menu 열기
                    UIManager.Instance.Open(MenuId.EscapeMenu);
                    break;

                case MenuId.EscapeMenu: // Escape Menu -> HUD 열기
                    UIManager.Instance.Open(MenuId.HUD);
                    if (cameraController != null) cameraController.OnMatchClick();
                    break;

                case MenuId.Settings: // Option 창 닫기
                    if (cameraController != null) cameraController.ToFrontViewCam();
                    UIManager.Instance.Open(MenuId.EscapeMenu);
                    break;
            }        
        }
    }
}