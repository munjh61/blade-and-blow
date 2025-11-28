using UnityEngine;

public class FollowRightHand : MonoBehaviour
{
    public Transform handTransform; // 따라갈 손
    [HideInInspector] public bool follow = false;

    // 디폴트 위치와 회전 저장
    public Transform defaultPosition;

    

    void LateUpdate()
    {

        if (follow && handTransform != null)
        {
            transform.position = handTransform.position;
        }
        else
        {
            // Follow가 아닐 때 디폴트 위치로 돌아가기
            transform.position = defaultPosition.position;
        }
    }
}
