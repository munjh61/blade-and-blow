using UnityEngine;

[CreateAssetMenu(fileName = "New Map", menuName = "Game/Map Data")]
public class MapData : ScriptableObject
{
    public string mapName;
    public Sprite mapSprite;
    public string mapDescription;
}