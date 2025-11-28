
using UnityEngine;
using TMPro;
public class RoomOptionsConfigurable :
  RoomConfigurable
{
    [Header("Room Settings")]
    public TMP_InputField roomNameField;
    public TMP_InputField passwordField;
    public TMP_InputField maxPlayersField;

    public override void ApplyToConfiguration(RoomConfiguration config)
    {
        if (roomNameField != null)
            config.roomName = roomNameField.text;
        if (passwordField != null)
            config.password = passwordField.text;
        if (maxPlayersField != null && int.TryParse(maxPlayersField.text, out int max))
            config.maxPlayers = max;
    }

    public override void LoadFromConfiguration(RoomConfiguration config)
    {
        if (roomNameField != null)
            roomNameField.text = config.roomName;
        if (passwordField != null)
            passwordField.text = config.password;
        if (maxPlayersField != null)
            maxPlayersField.text =
            config.maxPlayers.ToString();
    }
}