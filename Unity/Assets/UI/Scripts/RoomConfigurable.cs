using UnityEngine;

public abstract class RoomConfigurable : MonoBehaviour
{
    [HideInInspector]
    public RoomConfigurationGroup group;

    protected virtual void Awake()
    {
        if (group == null) group = GetComponentInParent<RoomConfigurationGroup>();
    }

    protected virtual void OnEnable()
    {
        group?.Register(this);
    }

    protected virtual void OnDisable()
    {
        group?.Unregister(this);
    }

    public abstract void ApplyToConfiguration(RoomConfiguration config);
    public abstract void LoadFromConfiguration(RoomConfiguration config);
}