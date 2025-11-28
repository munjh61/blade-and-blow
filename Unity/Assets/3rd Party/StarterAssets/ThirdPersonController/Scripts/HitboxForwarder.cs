using UnityEngine;

public class HitboxForwarder : MonoBehaviour
{
    private Enemy enemy;
    private ParticleSystem ps;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject otherRoot = other.transform.root.gameObject;
        GameObject thisRoot = this.transform.root.gameObject;
        if (otherRoot == thisRoot) return;
        enemy.OnHitboxTriggerEnter(ps, other);
    }
}

