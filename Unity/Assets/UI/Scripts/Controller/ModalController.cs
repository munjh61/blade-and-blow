using UnityEngine;
using TMPro;

public class ModalController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Awake()
    {
        if (!root) root = gameObject;
        root.SetActive(false); // 시작은 닫힘
    }

    public bool IsOpen => root && root.activeSelf;

    public void Show(string message)
    {
        if (messageText) messageText.text = $"{message}\n Press ESC to exit";
        root.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}
