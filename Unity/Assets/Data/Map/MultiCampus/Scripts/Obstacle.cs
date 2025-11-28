using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float lifetime;

    public void Initialize(Vector3 targetPosition, float moveSpeed, float lifeTime)
    {
        // 초기 방향을 한 번만 계산하여 고정
        direction = (targetPosition - transform.position).normalized;
        speed = moveSpeed;
        lifetime = lifeTime;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // 고정된 방향으로 계속 이동
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject); // 충돌하면 고양이 제거
        }
    }
}
