using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlatformerCharacterController : MonoBehaviour
{
    [Header("Grounding")]
    [SerializeField] private LayerMask GroundLayer;
    [SerializeField][Range(0.05f, 0.5f)] private float GroundCheckCircleRadius = 0.2f;
    [SerializeField] private Transform[] GroundChecks;
    
    [Header("Movement")]
    [SerializeField] private float JumpSpeed = 20;
    [SerializeField] private float GroundedMoveSpeed = 20;
    [SerializeField] private float UngroundedMoveSpeed = 20;
    [SerializeField] private float TerminalSpeed = 40;
    [SerializeField][Range(0, 1)] private float GroundedMoveSnapiness = 0.4f;
    [SerializeField][Range(0, 1)] private float UngroundedMoveSnapiness = 0.04f;
    [SerializeField] private Vector2 LandingImpulseMultiplier = new Vector2(0.05f, 0.05f);
    [SerializeField][Range(0, 3)] private int LandingImpulseToUse = 0;
    // [SerializeField] private Vector2 ImpulseOnLanding = new Vector2(50, 0);
    
    // Constants
    private const float MaxJumpQueueTime = 0.1f;

    // Cached references
    private Rigidbody2D rb;
    private InputAction moveAction;
    private Vector2 prevLinearVelocity;
    
    // States, counters, etc.
    private bool isGrounded, wasGrounded;
    private float lastJumpTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        InputSystem.actions.FindAction("Jump").performed += ctx => Jump();
    }

    private void FixedUpdate()
    {
        if (rb.IsAwake())
        {
            wasGrounded = isGrounded;
            isGrounded = GroundChecks.Any(t => Physics2D.OverlapCircle(t.position, GroundCheckCircleRadius, GroundLayer));
            if (wasGrounded != isGrounded) print("Now " + (isGrounded ? "grounded" : "not grounded"));
        
            if (!wasGrounded && isGrounded)
            {
                // To fix the 'stickiness' of landings, add an impulse relative to the previous x-velocity
                var landingImpulses = new float[]
                {
                    prevLinearVelocity.x * Mathf.Abs(prevLinearVelocity.y) * LandingImpulseMultiplier.x,
                    prevLinearVelocity.x * LandingImpulseMultiplier.x + Mathf.Sign(prevLinearVelocity.x) * Mathf.Abs(prevLinearVelocity.y) * LandingImpulseMultiplier.y,
                    prevLinearVelocity.magnitude * prevLinearVelocity.x * LandingImpulseMultiplier.x,
                    prevLinearVelocity.magnitude * Mathf.Sign(prevLinearVelocity.x) * Mathf.Abs(prevLinearVelocity.y) * LandingImpulseMultiplier.x
                };
                print($"x * |y| * m = {prevLinearVelocity.x} * {Mathf.Abs(prevLinearVelocity.y)} * {LandingImpulseMultiplier.x} = {landingImpulses[0]}");
                print($"x * m.x + sign(x) * |y| * m.y = {prevLinearVelocity.x} * {LandingImpulseMultiplier.x} + {Mathf.Sign(prevLinearVelocity.x)} *{Mathf.Abs(prevLinearVelocity.y)} * {LandingImpulseMultiplier.y} = {landingImpulses[1]}");
                print($"mag * x * m = {prevLinearVelocity.magnitude} * {prevLinearVelocity.x} * {LandingImpulseMultiplier.x} = {landingImpulses[2]}");
                print($"mag * sign(x) * |y| * m = {prevLinearVelocity.magnitude} * {Mathf.Sign(prevLinearVelocity.x)} * {Mathf.Abs(prevLinearVelocity.y)} * {LandingImpulseMultiplier.x} = {landingImpulses[3]}");
                rb.AddForceX(landingImpulses[LandingImpulseToUse], ForceMode2D.Impulse);

                // Handle queued jump
                if (Time.time < lastJumpTime + MaxJumpQueueTime) Jump();
            }
        }
        
        Move();
        prevLinearVelocity = rb.linearVelocity;
    }

    private void Jump()
    {
        // rb.AddForce(Vector2.up * JumpStrength * 2, ForceMode2D.Impulse);
        if (isGrounded)
        {
            rb.linearVelocityY = JumpSpeed * 2;
        }
        else
        {
            lastJumpTime = Time.time;
        }
    }

    private void Move()
    {
        var moveInput = moveAction.ReadValue<Vector2>();
        
        if (isGrounded)
        {
            rb.linearVelocityX = Mathf.Lerp(rb.linearVelocityX, moveInput.x * GroundedMoveSpeed, GroundedMoveSnapiness);
        }
        else
        {
            if (moveInput.x != 0)
            {
                // Allow non-zero x-inputs to influence ungrounded velocity 
                rb.linearVelocityX = Mathf.Lerp(rb.linearVelocityX, moveInput.x * UngroundedMoveSpeed, UngroundedMoveSnapiness);
            }
            
            // Clamp ungrounded speed
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, TerminalSpeed);
        }

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Debug.DrawLine(transform.position, transform.position + (Vector3) rb.linearVelocity / 4, Color.red, Time.fixedDeltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var t in GroundChecks)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(t.position, GroundCheckCircleRadius);
        }
    }
}
