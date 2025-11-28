using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject player;
    [Space]
    public Transform spawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject _playerArmature = PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
    }
    // Update is called once per frame
    void Update()
    {

    }
}
