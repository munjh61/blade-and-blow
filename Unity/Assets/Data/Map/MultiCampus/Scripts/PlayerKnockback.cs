using UnityEngine;
using System.Collections;
using StarterAssets;
using Photon.Pun;

public class PlayerKnockback : MonoBehaviour
{
    private StarterAssets.ThirdPersonControllerReborn playerController;
    private CharacterController characterController;
    private PhotonView pv;
    private bool isKnockedBack = false;
    
    private void Awake()
    {
        playerController = GetComponent<StarterAssets.ThirdPersonControllerReborn>();
        characterController = GetComponent<CharacterController>();
        pv = GetComponent<PhotonView>();
    }
    
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        // 로컬 플레이어만 넉백 적용
        if (pv != null && !pv.IsMine) return;
        
        if (!isKnockedBack)
        {
            StartCoroutine(KnockbackCoroutine(direction, force, duration));
        }
    }
    
    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        isKnockedBack = true;
        
        // 플레이어 컨트롤 비활성화
        bool wasControllerEnabled = false;
        if (playerController != null)
        {
            wasControllerEnabled = playerController.enabled;
            // playerController.enabled = false; // 필요시 주석 해제
        }
        
        Vector3 knockbackVelocity = direction * force;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            
            // 시간에 따른 넉백 속도 감소
            float falloff = 1f - (timer / duration);
            Vector3 currentVelocity = knockbackVelocity * falloff;
            
            // 중력 적용
            currentVelocity.y -= 9.81f * Time.deltaTime;
            
            // CharacterController로 이동
            if (characterController != null)
            {
                characterController.Move(currentVelocity * Time.deltaTime);
            }
            
            yield return null;
        }
        
        // 컨트롤 복구
        if (playerController != null && wasControllerEnabled)
        {
            playerController.enabled = true;
        }
        
        isKnockedBack = false;
    }
    
    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }
}