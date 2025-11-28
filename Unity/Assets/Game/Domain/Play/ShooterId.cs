using UnityEngine;

public class ShooterId : MonoBehaviour
{
    public int playerId { get; set; }
    public void setPlayerId(int playerId)
    {
        this.playerId = playerId;
    }
}
