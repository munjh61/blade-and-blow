using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshPro))]
[RequireComponent(typeof(BoxCollider))]
public class TMP3DButton : MonoBehaviour
{
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.92f, 0.96f, 1f);
    public Color pressedColor = new Color(0.75f, 0.86f, 1f);

    [Header("Outline (TMP SDF)")]
    [Range(0f, 1f)] public float normalOutline = 0.00f;
    [Range(0f, 1f)] public float hoverOutline = 0.18f;
    [Range(0f, 1f)] public float pressedOutline = 0.30f;

    [Header("Hover Scale")]
    [Tooltip("호버 시 스케일 배수")]
    public float hoverScaleMultiplier = 1.05f;
    [Tooltip("스케일 전환 속도(부드럽게)")]
    public float scaleLerpSpeed = 16f;

    [Header("Events")]
    public UnityEvent onClick;

    private TextMeshPro _tmp;
    private BoxCollider _col;
    private Vector3 _baseScale;
    private Material _runtimeMat;
    private bool _hover;
    private bool _pressed;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _col = GetComponent<BoxCollider>();
        _baseScale = transform.localScale;

        // 머티리얼 인스턴스화 (공유 머티 수정 방지)
        _runtimeMat = Instantiate(_tmp.fontMaterial);
        _tmp.fontMaterial = _runtimeMat;

        ApplyVisual(normalColor, normalOutline);
        FitColliderToText();
    }
    void Update()
    {
        // 스케일 부드럽게 보간
        var targetScale = _hover ? _baseScale * hoverScaleMultiplier : _baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);
    }

    public void FitColliderToText()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        if (_col == null) _col = GetComponent<BoxCollider>();

        _tmp.ForceMeshUpdate();

        // MeshRenderer의 월드 바운드를 로컬로 변환
        var r = _tmp.renderer;
        if (r == null) return;

        Bounds wb = r.bounds; // world bounds
        Vector3 localCenter = transform.InverseTransformPoint(wb.center);
        Vector3 localSize = transform.InverseTransformVector(wb.size);

        _col.center = localCenter;
        _col.size = localSize;
    }
    void ApplyVisual(Color color, float outline)
    {
        _tmp.color = color;
        _tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
        // 필요하면 여기서 Outline Color도 바꿀 수 있음:
        // _tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
    }
    void OnMouseEnter()
    {
        _hover = true;
        if (!_pressed) ApplyVisual(hoverColor, hoverOutline);
    }

    void OnMouseExit()
    {
        _hover = false;
        _pressed = false;
        ApplyVisual(normalColor, normalOutline);
    }

    void OnMouseDown()
    {
        _pressed = true;
        ApplyVisual(pressedColor, pressedOutline);
    }
    void OnMouseUpAsButton()
    {
        _pressed = false;
        ApplyVisual(_hover ? hoverColor : normalColor, _hover ? hoverOutline : normalOutline);
        onClick?.Invoke();
    }
}
