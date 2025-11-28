using UnityEngine;

[CreateAssetMenu(fileName = "UserSettingsDefaults", menuName = "Settings/Defaults")]
public class UserSettingsDefaults : ScriptableObject
{
    public UserSettings defaults = new UserSettings();
}