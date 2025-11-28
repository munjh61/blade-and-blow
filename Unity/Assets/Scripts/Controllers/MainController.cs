using UnityEngine;

public class MainController : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.Open(MenuId.MainMenu);
    }
}
