using Game.Domain;
using Photon.Pun;
using StarterAssets;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviourPun
{
    private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    private const int MAX_HEALTH_NUM = 100;
    private ThirdPersonControllerReborn _tpcR;
    private PlayerManagerPunBehaviour _pm;

    private void Awake()
    {
        if (maxHealth != MAX_HEALTH_NUM) maxHealth = MAX_HEALTH_NUM;

        currentHealth = maxHealth;
        _tpcR = GetComponent<ThirdPersonControllerReborn>();
        _pm = FindFirstObjectByType<PlayerManagerPunBehaviour>();
    }

    private IEnumerator OnDeathCoroutine()
    {
        _tpcR.OnDeath();
        yield return new WaitForSeconds(5f);
        gameObject.SetActive(false);
    }


    public void handleHit(ParticleSystem ps, Collider other)
    {
        OnHitboxTriggerEnter(ps, other);

    }
    

    public void OnHitboxTriggerEnter(ParticleSystem ps, Collider other)
    {
        Weapon weapon = other.GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.Log("Not weapon");
            return;
        }

        // 가변적인 viewId가 아닌, 
        // 방 생성부터 끝까지 유지되는 ActorNumber를 사용한다.
        int targetId = photonView.OwnerActorNr;
        //PlayerId targetPlayerId = _pm.LocalId;

        int attackerId;
        if (other.CompareTag("Melee"))
        {
            //근접 공격 -> 부모의 photonView의 owner로 찾음
            attackerId = other.GetComponentInParent<PhotonView>().OwnerActorNr;
        }
        else
        {
            //원거리 공격 -> 투사체의 shooterId로 찾음
            attackerId = other.GetComponent<ShooterId>().playerId;
            Destroy(other);
        }

        StartCoroutine(PlayHitParticle(ps));

        PlayerId targetPlayerId = new PlayerId(targetId);
        _pm.Master_RequestHit(targetPlayerId, weapon.damage, attackerId);
    }


    public IEnumerator PlayHitParticle(ParticleSystem ps)
    {
        if (ps != null)
        {
            ps.Play();
            yield return new WaitForSeconds(3f);
            ps.Stop();
        }
    }
}