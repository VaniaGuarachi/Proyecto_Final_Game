using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ColdSlimeProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.4f;
    [SerializeField] private float spinSpeed = 140f;

    private Vector2 velocity;
    private float lifeTimer;

    public void Launch(Vector2 direction, float speed, float projectileLifetime)
    {
        velocity = direction.normalized * speed;
        lifetime = projectileLifetime;
        lifeTimer = lifetime;
    }

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Collider2D hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;
        lifeTimer = lifetime;
    }

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Slime") && !other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
