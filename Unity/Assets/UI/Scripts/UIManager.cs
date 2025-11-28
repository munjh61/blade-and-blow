using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MenuId { 
    // 초기 진입
    MainMenu,
    
    // 게임 매칭 모드
    SelectMode,

    // 일반 게임 매칭 중 UI
    MatchInfoPanel,
    SelectWeapon,
    CurrentSelectedWeapon,

    // 비공개 게임 매칭 중 UI
    PrivateMatchMenus,
    JoinRoom,
    CreateRoom,
    Lobby,

    // 인게임 UI
    InGameUI,
    HUD,
    EscapeMenu,
    LeaveConfirmDialog,

    // 게임 설정
    Settings,

    // 로그인, 회원가입
    Greeting,
    Login,
    Signup,

    // 사용자 정보
    Record,

    //게이무 오버
    GameOver,

    GuestSignup
}

[Serializable]
public class MenuEntry
{
    public MenuId id;
    public GameObject prefab;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("MenuScene 설정")]
    public string menuSceneName = "MenuScene";

    [Header("메뉴 프리팹 (MenuScene Canvas 하위에 둘 수도, 여기서 참조해도 OK)")]
    public List<MenuEntry> menus = new();

    [Header("Menu Repositioning")]
    public Transform menuRootAnchor;
    public Transform OptionTargetAnchor;

    [Header("Player Detection")]
    [Tooltip("플레이어를 찾을 때 사용할 태그")]
    public string playerTag = "Player";
    [Tooltip("플레이어 탐색 간격(초)")]
    public float playerLookupInterval = 0.5f;

    private readonly Dictionary<MenuId, GameObject> _spawned = new();
    private Transform _menuRoot;
    private Scene _uiScene;
    private Coroutine _playerLookupCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 플레이어 anchor 연결 시도
        StartPlayerLookup();
    }

    private void Update()
    {
        if (menuRootAnchor==null || OptionTargetAnchor==null)
        {
            TryConnectPlayerAnchors();
        }        
    }

    private void OnDestroy()
    {
        if (_playerLookupCoroutine != null)
        {
            StopCoroutine(_playerLookupCoroutine);
            _playerLookupCoroutine = null;
        }
    }

    private void StartPlayerLookup()
    {
        if (_playerLookupCoroutine != null) StopCoroutine(_playerLookupCoroutine);
        _playerLookupCoroutine = StartCoroutine(PlayerLookupCoroutine());
    }

    private IEnumerator PlayerLookupCoroutine()
    {
        while (!HasValidAnchors())
        {
            TryConnectPlayerAnchors();
            yield return new WaitForSeconds(playerLookupInterval);
        }
    }

    private bool HasValidAnchors()
    {
        return menuRootAnchor != null && OptionTargetAnchor != null;
    }

    private void TryConnectPlayerAnchors()
    {
        // 1) 태그로 플레이어 찾기
        GameObject player = GameObject.FindWithTag(playerTag);

        // 2) 실패하면 타입으로 스캔
        if (player == null)
        {
            var tpc = FindFirstObjectByType<ThirdPersonControllerReborn>(FindObjectsInactive.Include);
            if (tpc != null) player = tpc.gameObject;
        }

        if (player == null) return;

        // 3) UIRoot 찾기
        Transform uiRoot = player.transform.Find("UIRoot");
        if (uiRoot == null) return;

        // 4) MenuRoot와 OptionViewTarget 찾아서 연결
        Transform menuRoot = uiRoot.Find("MenuRoot");
        Transform optionTarget = uiRoot.Find("OptionViewTarget");

        if (menuRoot != null) menuRootAnchor = menuRoot;
        else Debug.LogWarning("[UIManager] MenuRoot를 찾을 수 없습니다.");

        if (optionTarget != null) OptionTargetAnchor = optionTarget;
        else Debug.LogWarning("[UIManager] OptionViewTarget을 찾을 수 없습니다.");

        // 5) 모두 연결되면 코루틴 종료
        if (menuRootAnchor != null && OptionTargetAnchor != null && _playerLookupCoroutine != null)
        {
            StopCoroutine(_playerLookupCoroutine);
            _playerLookupCoroutine = null;
        }
    }

    /// <summary>
    /// 외부에서 수동으로 플레이어 anchor 연결을 재시도할 때 사용
    /// </summary>
    public void RefreshPlayerAnchors()
    {
        TryConnectPlayerAnchors();
        if (!HasValidAnchors())
        {
            StartPlayerLookup();
        }
    }

    private bool _menuSceneLoaded;

    public readonly struct UIOpen
    {
        public readonly MenuId id;
        public readonly object args;

        public UIOpen(MenuId id, object args = null) { this.id = id; this.args = args; }

        // 암시적 변환: MenuId -> UIOpen
        public static implicit operator UIOpen(MenuId id) => new UIOpen(id, null);
        // 암시적 변환: (MenuId, object) -> UIOpen   // 예: (MenuId.SelectWeapon, new SelectWeaponArgs{...})
        public static implicit operator UIOpen((MenuId id, object args) t) => new UIOpen(t.id, t.args);
    }

    public void Open(params UIOpen[] items)
    {
        if (items == null || items.Length == 0) return;

        EnsureOverlayLoaded(() =>
        {
            CloseAll();

            foreach (var it in items)
                Activate(it.id, it.args);
        });
    }

    private void PruneDead()
    {
        var dead = _spawned.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
        for (int i = 0; i < dead.Count; i++)
            _spawned.Remove(dead[i]);
    }

    public void EnsureOverlayLoaded(Action onReady = null)
    {
        if (_menuSceneLoaded && _uiScene.IsValid() && _uiScene.isLoaded)
        {
            PruneDead();
            onReady?.Invoke();
            return;
        }

        _spawned.Clear();
        _menuSceneLoaded = false;

        var op = SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Additive);
        op.completed += _ =>
        {
            _menuSceneLoaded = true;
            _uiScene = SceneManager.GetSceneByName(menuSceneName);

            foreach (var root in _uiScene.GetRootGameObjects())
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                foreach (var al in root.GetComponentsInChildren<AudioListener>(true)) al.enabled = false;
            }

            // MenuScene에서 Canvas/루트를 찾아둠
            _menuRoot = null;
            foreach (var root in _uiScene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    // 필요 시 부모 체인 활성화
                    var t = canvas.transform;
                    while (t != null) { if (!t.gameObject.activeSelf) t.gameObject.SetActive(true); t = t.parent; }
                    _menuRoot = canvas.transform;
                    break;
                }
            }

            PruneDead();
            onReady?.Invoke();
        };
    }

    public void Open(MenuId id)
    {
        EnsureOverlayLoaded(() =>
        {
            // 하나만 켜고 나머지는 끔
            CloseAll();

            var go = SpawnOrGet(id);
            if (go == null) return;

            go.SetActive(true);
            RepositionMenuElements(id);


            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }

            var c = go.GetComponentInChildren<Canvas>();
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
                c.sortingOrder = 5000;            

            Debug.Log($"[UIManager] Open {id} => {go.name}");
        });
    }

    public void OpenMultiple(params MenuId[] ids)
    {
        EnsureOverlayLoaded(() =>
        {
            CloseAll();

            foreach (var id in ids)
            {
                var go = SpawnOrGet(id);
                if (go == null) return;

                go.SetActive(true);
                RepositionMenuElements(id);

                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }

                var c = go.GetComponentInChildren<Canvas>();
                if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    c.sortingOrder = 5000;

                RepositionMenuElements(id);

                Debug.Log($"[UIManager] Open {id} => {go.name}");
            }
        });
    }

    public T Open<T>(MenuId id, object args = null) where T : Component
    {
        T result = null;
        EnsureOverlayLoaded(() =>
        {
            CloseAll();
            var go = SpawnOrGet(id);
            if (go == null) return;

            // Init을 먼저 호출하기 위해 잠시 비활성
            bool prevActive = go.activeSelf;
            go.SetActive(false);

            // Init (요청된 T 하나만 보장하고 싶다면 여기서만 호출하고, 아래 Activate에서는 호출 생략해도 됨)
            if (args != null)
            {
                // 우선 T에서 IInitializable을 찾고, 없으면 전체에서 찾음
                var target = go.GetComponent<T>() ?? go.GetComponentInChildren<T>(true);
                if (target is IInitializable initT) initT.Init(args);
                else
                {
                    // fallback: 모든 IInitializable에 브로드캐스트
                    foreach (var init in go.GetComponentsInChildren<IInitializable>(true))
                        init.Init(args);
                }
            }

            SetupCanvasFlags(go);            
            go.SetActive(true);
            RepositionMenuElements(id);

            result = go.GetComponent<T>() ?? go.GetComponentInChildren<T>(true);
        });
        return result;
    }
    
    public void Close(MenuId id)
    {
        if (_spawned.TryGetValue(id, out var go))
            go.SetActive(false);
    }

    public void CloseAll()
    {
        PruneDead();

        foreach (var kv in _spawned.ToArray())
        {
            var go = kv.Value;
            if (!go) { _spawned.Remove(kv.Key); continue; }
            if (go.activeSelf) go.SetActive(false);
        }
    }

    public bool IsOpen(MenuId id)
    {
        if (_spawned.TryGetValue(id, out var go) && go)
            return go.activeSelf;
        return false;
    }

    public List<MenuId> GetOpenMenus()
    {
        PruneDead();
        return _spawned.Where(kv => kv.Value != null && kv.Value.activeSelf).Select(kv => kv.Key).ToList();
    }

    public void Toggle(MenuId id)
    {
        EnsureOverlayLoaded(() =>
        {
            if (IsOpen(id)) Close(id);
            else Open(id);
        });
    }

    public T Get<T>(MenuId id) where T : Component
    {
        if (_spawned.TryGetValue(id, out var go) && go)
            return go.GetComponentInChildren<T>(true);
        return null;
    }

    private void Activate(MenuId id, object args)
    {
        var go = SpawnOrGet(id);
        if (go == null) return;

        // Init이 먼저 수행되도록 비활성 → Init → 활성 순서
        bool wasActive = go.activeSelf;
        go.SetActive(false);

        if (args != null)
        {
            // 패널 트리 내 모든 IInitializable에 브로드캐스트
            var initializables = go.GetComponentsInChildren<IInitializable>(true);
            for (int i = 0; i < initializables.Length; i++)
                initializables[i].Init(args);
        }

        SetupCanvasFlags(go);
        RepositionMenuElements(id);
        go.SetActive(true);

        Debug.Log($"[UIManager] Open {id} => {go.name}" + (args != null ? " (with args)" : ""));
    }

    private void SetupCanvasFlags(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }

        var c = go.GetComponentInChildren<Canvas>();
        if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay)
            c.sortingOrder = 5000;
    }

    private GameObject SpawnOrGet(MenuId id)
    {
        if (_spawned.TryGetValue(id, out var go))
        {
            if (go) return go;
            _spawned.Remove(id);
        }

        var src = GetPrefab(id);
        if (src == null)
        {
            Debug.LogError($"[UIManager] prefab missing for {id}");
            return null;
        }

        // 씬 오브젝트인지, 프리팹 에셋인지 구분
        if (src.scene.IsValid())
        {
            go = src;
            if (_menuRoot != null && go.transform.parent != _menuRoot)
                go.transform.SetParent(_menuRoot, false);
        }
        else
        {
            bool hasCanvas = src.GetComponentInChildren<Canvas>(true) != null;
            Transform parent = (!hasCanvas && _menuRoot != null) ? _menuRoot : null;
            go = Instantiate(src, parent);

            SetupCanvasEventCamera(go);

            // UI 전용 씬으로 귀속
            if (_uiScene.IsValid())
                SceneManager.MoveGameObjectToScene(go, _uiScene);
        }

        _spawned[id] = go;
        return go;
    }

    private void SetupCanvasEventCamera(GameObject menuObject)
    {
        Canvas canvas = menuObject.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            // Main Camera 찾아서 할당
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
            }
            else
            {
                Debug.LogWarning("[UIManager] Main Camera를 찾을 수 없습니다!");
            }
        }
    }
    private GameObject GetPrefab(MenuId id)
    {
        var e = menus.Find(x => x.id == id);
        return e != null ? e.prefab : null;
    }

    //현재 카메라와 플레이어 위치를 기준으로 UI 위치 재조정
    private void RepositionMenuElements(MenuId id)
    {
        Transform anchor = null;

        if (id == MenuId.Settings || id == MenuId.Record || id== MenuId.GuestSignup)
        {
            anchor = OptionTargetAnchor;
            var cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null) cameraController.ToOptionViewCam();
        }
        else
            anchor = menuRootAnchor;

        if (anchor == null) return;

        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.CompareTag("MenuRoot") && obj.scene.IsValid())
            {
                obj.transform.position = anchor.position;
                obj.transform.rotation = anchor.rotation;
            }
        }
    }
}
