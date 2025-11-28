using UnityEngine;
using Photon.Pun;

public class PhotonFollowRightHand : MonoBehaviourPun
{
    public Transform handTransform; // 따라갈 손
    [HideInInspector] public bool follow = false;

    public Transform defaultPosition; // 디폴트 위치

    void LateUpdate()
    {
        if (follow && handTransform != null)
        {
            transform.position = handTransform.position;
            transform.rotation = handTransform.rotation;
        }
        else
        {
            // Follow가 아닐 때 디폴트 위치로 돌아가기
            transform.position = defaultPosition.position;
        }
    }
}
