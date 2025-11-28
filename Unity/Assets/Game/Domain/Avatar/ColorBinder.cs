using System.Collections;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Game.Domain;

[RequireComponent(typeof(PhotonView))]
public class ColorBinder : MonoBehaviour
{
    [Tooltip("없으면 자식에서 자동으로 찾음")]
    public ColorController[] colorControllers;

    private PhotonView _view;
    private TeamManager _team;
    private PhotonNetworkManager _net;
    private bool _ready;

    private void Awake()
    {
        _view = GetComponent<PhotonView>();
        if (colorControllers.Length == 0) colorControllers = GetComponentsInChildren<ColorController>(false);
        _team = TeamManager.Instance;
        _net  = PhotonNetworkManager.Instance;
    }

    private void OnEnable()
    {
        StartCoroutine(InitNextFrame());
        if (_net != null)
        {
            _net.RoomPropertiesUpdated += OnRoomPropsUpdatedForward;
        }
    }

    private void OnDisable()
    {
        if (_net != null)
        {
            _net.RoomPropertiesUpdated -= OnRoomPropsUpdatedForward;
        }
    }

    private IEnumerator InitNextFrame()
    {
        yield return null;
        ApplyFromRoomProps();
        _ready = true;
    }

    private void ApplyFromRoomProps()
    {
        if(colorControllers.Length == 0) return;
        if (_view == null || _team == null) return;
        var owner = _view.Owner;
        if (owner == null) return;

        if (_team.TryGetTeamFromRoomProps(owner.ActorNumber, out var team))
            foreach (var colorController in colorControllers)
                colorController.ApplyTeam(team);
        else
            foreach (var colorController in colorControllers)
                colorController.ApplyTeam(TeamId.None);
    }

    private void OnRoomPropsUpdatedForward(Hashtable changed)
    {
        if (!_ready || _view == null) return;
        foreach (DictionaryEntry kv in changed)
        {
            if (kv.Key is string k && k.StartsWith(TeamManager.KEY_TEAM_PREFIX))
            {
                if (int.TryParse(k.Substring(TeamManager.KEY_TEAM_PREFIX.Length), out int actor))
                {
                    if (_view.Owner != null && actor == _view.Owner.ActorNumber)
                    {
                        ApplyFromRoomProps();
                        break;
                    }
                }
            }
        }
    }
}
