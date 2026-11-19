
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;

    public float movHor = 1f;
    public float speed = 3f;

    public bool isGroundFloor = true;
    public bool isGroundFront = false;

    public LayerMask groundLayer;
    public float frontGrnRayDist = 0.25f;
    public float floorCheckY = 0.52f;
    public float frontCheck = 0.51f;
    public float frontDist = 0.1f;

    public int scoreGive = 50;

    private RaycastHit2D hit;

    void Start()
    {
        // Obtener componentes
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Iniciar movimiento
        if (movHor == 0)
            movHor = 1f;
    }

    void Update()
    {
        float direction = Mathf.Sign(movHor);

        // Evitar caer en precipicio
        Vector2 floorOrigin;

        if (col != null)
        {
            floorOrigin = new Vector2(
                col.bounds.center.x +
                direction * (col.bounds.extents.x + 0.05f),
                col.bounds.min.y + 0.1f
            );
        }
        else
        {
            floorOrigin = new Vector2(
                transform.position.x +
                direction * frontCheck,
                transform.position.y - floorCheckY
            );
        }

        isGroundFloor = Physics2D.Raycast(
            floorOrigin,
            Vector2.down,
            frontGrnRayDist,
            groundLayer
        );

        // Chocar con pared
        Vector2 wallOrigin = col != null
            ? new Vector2(col.bounds.center.x, col.bounds.center.y)
            : (Vector2)transform.position;

        isGroundFront = Physics2D.Raycast(
            wallOrigin,
            Vector2.right * direction,
            frontCheck,
            groundLayer
        );

        // Chocar con otro enemigo
        hit = Physics2D.Raycast(
            wallOrigin,
            Vector2.right * direction,
            frontCheck + frontDist
        );

        bool enemyFront = false;

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Enemy") &&
                hit.collider.gameObject != gameObject)
            {
                enemyFront = true;
            }
        }

        // Cambiar de direccion
        if (!isGroundFloor || isGroundFront || enemyFront)
        {
            movHor = movHor * -1f;
        }

        // Girar enemigo
        Vector3 scale = transform.localScale;

        if (movHor < 0)
            scale.x = -Mathf.Abs(scale.x);
        else if (movHor > 0)
            scale.x = Mathf.Abs(scale.x);

        transform.localScale = scale;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        rb.velocity = new Vector2(
            movHor * speed,
            rb.velocity.y
        );
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();

            if (player == null) return;

            // Pisoton: si los pies del Player estan por encima del centro del enemigo, lo derrota
            if (IsStompedBy(collision.collider))
            {
                Game.obj.addScore(scoreGive);

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxEnemyDefeat);

                player.Bounce();
                getKilled();
                return;
            }

            // Dañar al Player
            player.TakeDamage();
        }
    }

    private bool IsStompedBy(Collider2D playerCol)
    {
        if (col == null || playerCol == null) return false;

        return playerCol.bounds.min.y > col.bounds.center.y;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player.obj.TakeDamage();
        }
    }

    private void getKilled()
    {
        gameObject.SetActive(false);
    }

    // Mostrar detecciones en Scene
    private void OnDrawGizmosSelected()
    {
        float direction = movHor < 0 ? -1f : 1f;

        Collider2D enemyCollider = GetComponent<Collider2D>();

        Vector2 floorOrigin;
        Vector2 wallOrigin;

        if (enemyCollider != null)
        {
            floorOrigin = new Vector2(
                enemyCollider.bounds.center.x +
                direction * (enemyCollider.bounds.extents.x + 0.05f),
                enemyCollider.bounds.min.y + 0.1f
            );

            wallOrigin = enemyCollider.bounds.center;
        }
        else
        {
            floorOrigin = new Vector2(
                transform.position.x + direction * frontCheck,
                transform.position.y - floorCheckY
            );

            wallOrigin = transform.position;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            floorOrigin,
            floorOrigin + Vector2.down * frontGrnRayDist
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            wallOrigin,
            wallOrigin + Vector2.right * direction * frontCheck
        );
    }
}
