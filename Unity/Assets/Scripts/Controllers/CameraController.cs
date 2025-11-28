using Cinemachine;
using NUnit.Framework;
using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera followCam;
    public CinemachineVirtualCamera frontViewCam;
    public CinemachineVirtualCamera optionViewCam;

    [Header("Player Lookup")]
    [Tooltip("플레이어를 찾을 때 사용할 태그. 프리팹에 이 태그를 지정하세요.")]
    public string playerTag = "Player";
    [Tooltip("플레이어가 나중에 생성되면 이 시간 간격으로 탐색합니다.")]
    public float lookupInterval = 0.5f;

    private ThirdPersonControllerReborn _playerController;
    private StarterAssetsInputs _starterInputs;
    private PlayerInput _playerInput;
    private Coroutine _lookupRoutine;
    private bool isFPSMode = false;

    void Awake()
    {
        TryResolvePlayerRefs();
        if (!HasPlayerRefs())
            _lookupRoutine = StartCoroutine(LookupPlayerLoop());

        // 씬에 따라 초기 모드 적용
        ApplyInitialModeIfPossible();
    }

    private IEnumerator LookupPlayerLoop()
    {
        while (!HasPlayerRefs())
        {
            TryResolvePlayerRefs();
            yield return new WaitForSeconds(lookupInterval);
        }
        // 찾자마자 초기 모드 적용
        ApplyInitialModeIfPossible();
    }
    private bool HasPlayerRefs()
    {
        return _playerController != null && _starterInputs != null && _playerInput != null;
    }

    private void TryResolvePlayerRefs()
    {
        // 1) 태그로 찾기
        GameObject go = GameObject.FindWithTag(playerTag);

        // 2) 실패하면 타입으로 스캔(비활성 포함)
        if (go == null)
        {
            var tpc = UnityEngine.Object.FindFirstObjectByType<ThirdPersonControllerReborn>(FindObjectsInactive.Include);
            if (tpc != null) go = tpc.gameObject;
        }

        if (go != null)
        {
            _playerController = go.GetComponent<ThirdPersonControllerReborn>();
            _starterInputs = go.GetComponent<StarterAssetsInputs>();
            _playerInput = go.GetComponent<PlayerInput>();

            // UIRoot에서 view와 target 찾아서 카메라에 연결
            if(optionViewCam.LookAt == null || optionViewCam.Follow == null || frontViewCam.Follow == null || frontViewCam.LookAt == null)
                SetupCameraTargets(go);
        }
    }

    private void SetupCameraTargets(GameObject player)
    {
        // UIRoot 찾기
        Transform uiRoot = player.transform.Find("UIRoot");
        if (uiRoot == null) return;

        // UIRoot 하위에서 view와 target 찾기
        Transform frontRoot = uiRoot.Find("FrontViewRoot");
        Transform frontTarget = uiRoot.Find("FrontViewTarget");
        Transform optionRoot = uiRoot.Find("OptionViewRoot");
        Transform optionTarget = uiRoot.Find("OptionViewTarget");

        if (frontRoot == null || frontTarget == null || optionRoot == null || optionTarget == null)
            return;

        // Cinemachine 카메라들에 Follow와 LookAt 설정
        if (frontViewCam != null)
        {
            frontViewCam.Follow = frontRoot;
            frontViewCam.LookAt = frontTarget;
        }

        if (optionViewCam != null)
        {
            optionViewCam.Follow = optionRoot;
            optionViewCam.LookAt = optionTarget;
        }
    }

    private void ApplyInitialModeIfPossible()
    {
        if (IsPlayScene()) EnableGameplayInput();
        else DisableGameplayInput();
    }

    void Update()
    {
        //PlayScene에서는 Alt키로 UI 조작 모드로 전환 불가
        if (isFPSMode && !IsPlayScene())
        {
            bool isAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (isAlt)
            {
                // Alt 키를 누르면 UI 조작 모드
                if (_starterInputs != null)
                {
                    _starterInputs.cursorLocked = false;
                    _starterInputs.cursorInputForLook = false;
                }
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // Alt 키를 떼면 FPS Look 모드
                if (_starterInputs != null)
                {
                    _starterInputs.cursorLocked = true;
                    _starterInputs.cursorInputForLook = true;
                }
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void EnableGameplayInput()
    {
        if (_starterInputs != null)
        {
            _starterInputs.cursorLocked = true;
            _starterInputs.cursorInputForLook = true;
            _starterInputs.enabled = true;
        }

        if (_playerInput != null)
        {
            _playerInput.enabled = true;
            if (_playerInput.currentActionMap == null && _playerInput.actions != null)
            {
                var mapName = _playerInput.defaultActionMap;
                var map = !string.IsNullOrEmpty(mapName)
                    ? _playerInput.actions.FindActionMap(mapName, true)
                    : (_playerInput.actions.actionMaps.Count > 0 ? _playerInput.actions.actionMaps[0] : null);
                if (map != null) _playerInput.SwitchCurrentActionMap(map.name);
            }
            _playerInput.ActivateInput();
            _playerInput.actions?.Enable();
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (_playerController != null)
            _playerController.enabled = true;
    }

    private void DisableGameplayInput()
    {
        if (_starterInputs != null)
        {
            _starterInputs.cursorLocked = false;
            _starterInputs.cursorInputForLook = false;
            _starterInputs.enabled = false;
        }

        if (_playerInput != null)
        {
            _playerInput.DeactivateInput();
            _playerInput.enabled = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (_playerController != null)
            _playerController.enabled = false;
    }

    public void OnMatchClick()
    {
        // 혹시 아직 못 찾았으면 한 번 더 시도
        if (!HasPlayerRefs()) TryResolvePlayerRefs();

        // 카메라 전환
        if (frontViewCam) frontViewCam.Priority = 0;
        if (followCam) followCam.Priority = 20;

        // 인게임 모드로
        EnableGameplayInput();

        isFPSMode = true;
    }

    public void ToFrontViewCam()
    {
        // 혹시 아직 못 찾았으면 한 번 더 시도
        if (!HasPlayerRefs()) TryResolvePlayerRefs();

        if (followCam) followCam.Priority = 0;
        if (frontViewCam) frontViewCam.Priority = 20;

        // 매칭 모드 선택 화면으로
        DisableGameplayInput();

        isFPSMode = false;
    }

    public void ToOptionViewCam()
    {
        if (frontViewCam) frontViewCam.Priority = 0;
        if (optionViewCam) optionViewCam.Priority = 20;
    }
    
    public bool IsPlayScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene == "PlayScene";
    }
}
