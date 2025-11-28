using UnityEngine;


public class AimStateBehaviour : StateMachineBehaviour
{
    private FollowRightHand followScript;

    // 씬에 있는 오브젝트 이름
    public string followObjectName = "WB.string";

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (followScript == null)
        {
            GameObject obj = GameObject.Find(followObjectName);
            if (obj != null)
            {
                followScript = obj.GetComponent<FollowRightHand>();

                
            }
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
