using Game.Net;
using Game.Net.Pun;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class AvatarBootstrap : MonoBehaviour, IPunInstantiateMagicCallback
{
    private PhotonView _view;
    private INetAdapter _net;
    private AvatarRegistry.Handle _handle;

    [Header("Remote Interp")]
    public float netPosLerp = 12f;
    public float netRotLerp = 10f;

    [Header("Preview Mode")]
    [Tooltip("MainScene에서는 항상 로컬 프리뷰(네트워크 비참여)로 동작")]
    public bool forceLocalPreviewInMain = true;

    private const string MAIN_SCENE_NAME = "MainScene";
    private const int PREVIEW_ACTOR = 100000001;
    private int _registeredActor = 0;
    private bool _isPreview;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();

        var drv = GetComponent<NetReplicationDriver>() ?? gameObject.AddComponent<NetReplicationDriver>();
        drv.netPosLerp = netPosLerp;
        drv.netRotLerp = netRotLerp;

        _isPreview = ShouldRunAsPreview();

        if (_isPreview)
            ConfigurePreview(drv);
        else
            ConfigureNetwork();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (_isPreview) return;
        RegisterHandle(_view.OwnerActorNr);
    }

    private void OnDestroy()
    {
        if (_registeredActor != 0 && _handle != null)
        {
            AvatarRegistry.Unregister(_registeredActor, _handle);
            _registeredActor = 0;
        }
    }

    /// <summary>
    /// 프리뷰 모드인지 확인
    /// </summary>
    /// <returns>bool isPreview ? true : false</returns>
    private bool ShouldRunAsPreview()
    {
        return forceLocalPreviewInMain && SceneManager.GetActiveScene().name == MAIN_SCENE_NAME;
    }

    /// <summary>
    /// 프리뷰 모드로 설정
    /// Instantiate되지 않으므로, 직접 Register 호출
    /// </summary>
    private void ConfigurePreview(NetReplicationDriver drv)
    {
        if (_view) _view.enabled = false;
        _net = null;

        drv.SetWriteAuthority(true);
        var tpc = GetComponent<StarterAssets.ThirdPersonControllerReborn>();
        if (tpc)
        {
            tpc.SetAuthority(true);
        }

        var binder = GetComponent<CameraBinder>();
        if (binder) binder.SetMode(CameraBinder.Mode.Disabled);
        RegisterHandle(PREVIEW_ACTOR);
    }

    /// <summary>
    /// GameSceneInitializer에서 instantiate되어서 OnPhotonInstantiate에서 register되므로 현재는 등록하지 않음
    /// 단순 네트워크 활성과 카메라 바인딩 모드만 설정
    /// </summary>
    private void ConfigureNetwork()
    {
        var pun = EnsurePunAdapter();
        if (pun && _view) SetupObservedComponents(_view, pun);

        var binder = GetComponent<CameraBinder>();
        if (binder) binder.SetMode(CameraBinder.Mode.OwnerOnly);
    }

    private PunNetAdapter EnsurePunAdapter()
    {
        _net = GetComponent<INetAdapter>();
        if (_net is PunNetAdapter existing) return existing;

        var pun = gameObject.AddComponent<PunNetAdapter>();
        _net = pun;
        return pun;
    }

    private static void SetupObservedComponents(PhotonView view, Component observed)
    {
        var list = view.ObservedComponents ?? new List<Component>();
        if (!list.Contains(observed)) list.Insert(0, observed);
        view.ObservedComponents = list;
        view.Synchronization = ViewSynchronization.UnreliableOnChange;
    }

    private void RegisterHandle(int actorNumber)
    {
        _handle = new AvatarRegistry.Handle
        {
            go = gameObject,
            view = _view,
            ce = GetComponent<CharacterEquipmentReborn>(),
            tpc = GetComponent<StarterAssets.ThirdPersonControllerReborn>(),
        };
        _registeredActor = actorNumber;
        AvatarRegistry.Register(_registeredActor, _handle);
    }
}
