using UnityEngine;
using Photon.Pun;

public class PhotonAimStateBehaviour : StateMachineBehaviour
{
    private PhotonFollowRightHand followScript;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (followScript == null)
        {
            followScript = animator.GetComponentInChildren<PhotonFollowRightHand>();
        }

        if (followScript != null)
            followScript.follow = true;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (followScript != null)
            followScript.follow = false;
    }
}
