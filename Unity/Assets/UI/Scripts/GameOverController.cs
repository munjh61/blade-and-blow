using UnityEngine;
using System;

public class GameOverController : MonoBehaviour
{
    public TMP2DButton RematchButton;
    public TMP2DButton LeaveButton;

    public void Start()
    {
        CameraController camController = FindObjectOfType<CameraController>();
        if (camController != null)
        {
            camController.ToFrontViewCam();
        }
    }

    private void OnEnable()
    {
        if (RematchButton != null)
            RematchButton.onClick.AddListener(OnRematchClick);

        if (LeaveButton != null)
            LeaveButton.onClick.AddListener(OnLeaveClick);
    }

    private void OnDisable()
    {
        if (RematchButton != null)
            RematchButton.onClick.RemoveAllListeners();

        if (LeaveButton != null)
            LeaveButton.onClick.RemoveAllListeners();
    }

    private void OnLeaveClick()
    {
        if (GameExitManager.Instance != null)
        {
            // 콜백 없이 LeaveGame을 호출하면, 방 퇴장 후 메인 메뉴 씬으로 이동합니다.
            // GameExitManager가 모든 UI를 닫아주므로 여기서 UI를 따로 제어할 필요가 없습니다.
            GameExitManager.Instance.LeaveGame();
        }
        else
        {
            Debug.LogError("[GameOverController] GameExitManager instance not found!");
        }
    }

    private void OnRematchClick()
    {
        if (GameExitManager.Instance != null)
        {
            GameExitManager.Instance.RequestRematch();
        }
        else
        {
            Debug.LogError("GameExitManager not found! Cannot request rematch.");
        }
    }
}
