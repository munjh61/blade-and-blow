using System.Collections;
using UnityEngine;

public struct WeaponContext
{
    public Transform cameraTransform;
    public float chargeDuration;
    //내가 직접 쏠 땐 필요 없지만 다른 사람이 쏘는 것에 대해 누가 쏜지 저장
    public int shooterId;   
}

public class Weapon : MonoBehaviour
{
    public enum Type { Sword, Bow, Wand, Arrow }
    public Type type;
    public int damage;
    public float rate;
    public BoxCollider meleeArea;

    [Header("Bow Settings")]
    public GameObject arrowPrefab;      // 발사할 화살 프리팹
    public GameObject arrowSpawnPoint;   // 화살이 생성될 위치(활의 끝)
    public float arrowMinSpeed = 30f;  // 화살 최소 속도
    public float arrowMaxSpeed = 60f;      // 화살 최대 속도

    public GameObject fireballPrefab;      // 발사할 화염구 프리팹
    public GameObject fireballSpawnPoint;   // 화살이 생성될 위치
    public float fireballSpeed = 15f;      // 화염구 최소 속도

    // 활, 마법 최소 차징타입 도입용 변수
    public float _bowMinChargeTime = 1f; // 최소 차징타임 (초)
    public float _bowMaxChargeTime = 4f; // 최대 차징타임 (초)

    public float _wandMinChargeTime = 1.5f; // 최소 차징타임 (초)


    public void Use(WeaponContext context)
    {
        if (type == Type.Sword)
        {
            StopCoroutine("Swing");
            StartCoroutine("Swing");
        }
        else if (type == Type.Bow)
        {
            // Bow attack logic here
            ShootArrow(context);
        }
        else if (type == Type.Wand)
        {
            // Wand attack logic here
            ShootFireball(context);
        }
    }

    private IEnumerator Swing(){ 
        yield return new WaitForSeconds(0.58f);
        meleeArea.enabled = true;
        yield return new WaitForSeconds(1f);
        meleeArea.enabled = false;
    }

    private void ShootArrow(WeaponContext context)
    {
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("Arrow prefab or spawn point not assigned!");
            return;
        }
        Vector3 fireDir = context.cameraTransform.forward;
        Quaternion spawnRotation = Quaternion.LookRotation(fireDir) * Quaternion.Euler(90f, 0f, 0f);
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.transform.position, spawnRotation);
        if (context.shooterId != -1)    //-1이면 자신임
        {
            arrow.GetComponent<ShooterId>().setPlayerId(context.shooterId);
        }
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = fireDir * calculateSpeed(arrowMinSpeed, arrowMaxSpeed, context.chargeDuration, _bowMinChargeTime, _bowMaxChargeTime);
        }
    }

    private void ShootFireball(WeaponContext context)
    {
        if (fireballPrefab == null || fireballSpawnPoint == null)
        {
            Debug.LogWarning("fireball prefab or spawn point not assigned!");
            return;
        }

        Vector3 fireDir = context.cameraTransform.forward;
        Quaternion spawnRotation = Quaternion.LookRotation(fireDir);// * Quaternion.Euler(90f, 0f, 0f);
        GameObject arrow = Instantiate(fireballPrefab, fireballSpawnPoint.transform.position, spawnRotation);
        if (context.shooterId != -1)    //-1이면 자신임
        {
            arrow.GetComponent<ShooterId>().setPlayerId(context.shooterId);
        }
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = fireDir * fireballSpeed;
        }
    }

    private float calculateSpeed(float minSpeed, float maxSpeed, float chargeDuration, float minChargeTime, float maxChargeTime)
    {
        // 차징 시간 보정
        float clampedCharge = Mathf.Clamp(chargeDuration, minChargeTime, maxChargeTime);

        // 0 ~ 1 사이로 정규화
        float chargePercent = (clampedCharge - minChargeTime) / (maxChargeTime - minChargeTime);

        // 속도 계산 (최대 Speed까지만)
        return Mathf.Lerp(minSpeed, maxSpeed, chargePercent);

    }
}
