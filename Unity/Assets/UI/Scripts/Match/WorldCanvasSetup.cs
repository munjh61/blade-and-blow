using UnityEngine;

public class WorldCanvasSetup : MonoBehaviour
{
    private void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                Camera mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
                if (mainCamera != null)
                {
                    canvas.worldCamera = mainCamera;
                    Debug.Log($"[WorldCanvasSetup] {gameObject.name}의 Event Camera 자동 설정");
                }
            }
        }
    }
}