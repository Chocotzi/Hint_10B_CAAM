using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player obj;

    public int lives = 3;
    [SerializeField] private float fallDeathY = -6f;

    public bool isGrounded = false;
    public bool isMoving = false;
    public bool isImmune = false;

    public float speed = 5f;
    public float jumpForce = 10f;
    public float movHor;

    public float immuneTimeCnt = 0f;
    public float immuneTime = 0.5f;

    public LayerMask groundLayer;
    public float radius = 0.3f;
    public float groundRayDist = 0.5f;
    public float landMinSpeed = 2f; // velocidad minima de impacto para sonar el aterrizaje

    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer spr;

    private Collider2D col;
    private bool hasMovingParameter;
    private bool hasGroundedParameter;

    private void Awake()
    {
        obj = this;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Verificar parametros del Animator
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == "IsMoving" &&
                    param.type == AnimatorControllerParameterType.Bool)
                    hasMovingParameter = true;

                if (param.name == "IsGrounded" &&
                    param.type == AnimatorControllerParameterType.Bool)
                    hasGroundedParameter = true;
            }
        }
    }

    void Update()
    {
        // No leer controles mientras el juego esta en pausa
        if (Game.obj != null && Game.obj.gamePaused) return;

        // Movimiento horizontal
        movHor = Input.GetAxisRaw("Horizontal");

        isMoving = (movHor != 0);

        // Detectar el suelo desde los pies
        if (col != null)
        {
            Vector2 feet = new Vector2(
                col.bounds.center.x,
                col.bounds.min.y - 0.05f
            );

            isGrounded = Physics2D.OverlapCircle(
                feet,
                0.12f,
                groundLayer
            ) != null;
        }
        else
        {
            isGrounded = false;
        }

        // Saltar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        // Actualizar animaciones solo si existen
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            if (hasMovingParameter)
                anim.SetBool("IsMoving", isMoving);

            if (hasGroundedParameter)
                anim.SetBool("IsGrounded", isGrounded);
        }

        // Girar personaje
        flip(movHor);

        // Control de inmunidad
        if (isImmune)
        {
            immuneTimeCnt -= Time.deltaTime;

            if (immuneTimeCnt <= 0f)
            {
                isImmune = false;
                immuneTimeCnt = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (rb.position.y < fallDeathY)
        {
            Game.obj.gameOver();
            return;
        }

        rb.velocity = new Vector2(
            movHor * speed,
            rb.velocity.y
        );
    }

    public void Jump()
    {
        if (!isGrounded) return;

        rb.velocity = new Vector2(
            rb.velocity.x,
            jumpForce
        );

        // SFX de salto con variacion de pitch
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxJump);
    }

    // Rebote al pisar a un enemigo
    public void Bounce()
    {
        rb.velocity = new Vector2(
            rb.velocity.x,
            jumpForce * 0.7f
        );
    }

    // Aterrizaje: choque contra el suelo viniendo desde arriba
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;
        if (collision.relativeVelocity.y < landMinSpeed) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxLand);
                break;
            }
        }
    }

    private void flip(float _xValue)
    {
        Vector3 theScale = transform.localScale;

        if (_xValue < 0)
            theScale.x = -Mathf.Abs(theScale.x);
        else if (_xValue > 0)
            theScale.x = Mathf.Abs(theScale.x);

        transform.localScale = theScale;
    }

    // Recibir daño
    public void TakeDamage()
    {
        if (isImmune || lives <= 0) return;

        lives--;

        // SFX de danio recibido
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxDamage);

        Debug.Log("Vidas restantes: " + lives);

        if (lives <= 0)
        {
            Game.obj.gameOver();
            return;
        }

        isImmune = true;
        immuneTimeCnt = immuneTime;
    }

    // Compatibilidad con el metodo original
    public void getDamage()
    {
        TakeDamage();
    }

    public void addLive()
    {
        lives++;

        if (lives > Game.obj.maxLives)
            lives = Game.obj.maxLives;
    }

    private void OnDestroy()
    {
        if (obj == this)
            obj = null;
    }
}
