using Game.Domain;
using UnityEngine;

/// <summary>
/// 팀 상태에 따라 머티리얼의 색상만 바꾼다.
/// Material 인스턴스 복제 없이 MaterialPropertyBlock으로 안전하게 적용.
/// </summary>
[DisallowMultipleComponent]
public class ColorController : MonoBehaviour
{
    [Header("Target")]
    public Renderer target;
    public TeamManager teamManager;

    [Header("Property Names")]
    public string[] colorPropertyNames = new[] { "_BaseColor", "_Color", "_TintColor" };

    [Header("Team Colors")]
    public Color neutral = Color.white;
    public Color red = new Color(0.95f, 0.25f, 0.25f);
    public Color blue = new Color(0.25f, 0.55f, 0.95f);

    [Header("Optional")]
    public int materialIndex = -1;

    private MaterialPropertyBlock _mpb;
    private int _colorId = -1;

    private void Awake()
    {
        if (!target) target = GetComponent<Renderer>();
        if (!teamManager) teamManager = TeamManager.Instance;

        _mpb = new MaterialPropertyBlock();
        ResolveColorId();
    }

    /// <summary>팀 지정 및 적용</summary>
    public void ApplyTeam(TeamId team)
    {
        if (!target || _colorId == -1) return;

        var c = TeamToColor(team);

        if (materialIndex >= 0 && materialIndex < target.sharedMaterials.Length)
        {
            target.GetPropertyBlock(_mpb, materialIndex);
            _mpb.SetColor(_colorId, c);
            target.SetPropertyBlock(_mpb, materialIndex);
        }
        else
        {
            target.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, c);
            target.SetPropertyBlock(_mpb);
        }
    }

    public void ClearOverride()
    {
        if (!target) return;
        if (materialIndex >= 0 && materialIndex < target.sharedMaterials.Length)
            target.SetPropertyBlock(null, materialIndex);
        else
            target.SetPropertyBlock(null);
    }

    private void ResolveColorId()
    {
        _colorId = -1;
        if (!target) return;

        var mats = target.sharedMaterials;
        foreach (var tryName in colorPropertyNames)
        {
            var id = Shader.PropertyToID(tryName);
            foreach (var m in mats)
            {
                if (m != null && m.HasProperty(id))
                {
                    _colorId = id;
                    return;
                }
            }
        }
    }

    private Color TeamToColor(TeamId team) => team switch
    {
        TeamId.Red => red,
        TeamId.Blue => blue,
        _ => neutral
    };
}
