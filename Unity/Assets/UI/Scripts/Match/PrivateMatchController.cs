using UnityEngine;
using Cinemachine;
using StarterAssets;
using System.Diagnostics;
using UnityEngine.UI;
using Unity.VisualScripting;

public class PrivateMatchController : MonoBehaviour
{
    [Header("Menu Button References")]
    public TMP2DButton JoinRoomButton;
    public TMP2DButton CreateRoomButton;
    public TMP2DButton backButton;

    private void OnEnable()
    {
        if (JoinRoomButton != null)
            JoinRoomButton.onClick.AddListener(OnJoinRoomClick);

        if (CreateRoomButton != null)
            CreateRoomButton.onClick.AddListener(OnCreateRoomClick);

        if (backButton != null)
            backButton.onClick.AddListener(BackToSelectModeMenu);
    }

    private void OnDisable()
    {
        if (JoinRoomButton != null)
            JoinRoomButton.onClick.RemoveAllListeners();

        if (CreateRoomButton != null)
            CreateRoomButton.onClick.RemoveAllListeners();

        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
    }

    public void OnJoinRoomClick()
    {
        UIManager.Instance.Open(MenuId.JoinRoom);
    }

    public void OnCreateRoomClick()
    {
        UIManager.Instance.Open(MenuId.CreateRoom);
    }

    public void BackToPrivateMatchButton()
    {
        UIManager.Instance.Open(MenuId.PrivateMatchMenus);
    }

    public void BackToSelectModeMenu()
    {
        UIManager.Instance.Open(MenuId.SelectMode);
    }

    public void OnCancelClick()
    {
        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.ToFrontViewCam();
        }

        Invoke("BackToMatchSelection", 1.0f);
    }


}
