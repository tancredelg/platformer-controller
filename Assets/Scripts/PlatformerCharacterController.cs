using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlatformerCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float JumpSpeed = 20;
    [SerializeField] private float GroundedMoveSpeed = 20;
    [SerializeField] private float UngroundedMoveSpeed = 20;
    [SerializeField] private float TerminalSpeed = 40;
    [SerializeField][Range(0, 1)] private float GroundedMoveSnapiness = 0.4f;
    [SerializeField][Range(0, 1)] private float UngroundedMoveSnapiness = 0.04f;
    [SerializeField] private float LandingImpulseMultiplier = 0.04f;
    
    [Header("Grounding")]
    [SerializeField] private LayerMask GroundLayer;
    [SerializeField][Range(0.05f, 0.5f)] private float GroundCheckCircleRadius = 0.2f;
    [SerializeField] private Transform[] GroundChecks;

    [Header("Holding & Carrying")]
    [SerializeField] private Transform PrimaryHold;

    // Constants
    private const float MaxJumpQueueTime = 0.1f;

    // Cached references
    private Rigidbody2D rb;
    private InputAction moveAction, pointAction;
    private Vector2 prevLinearVelocity;
    private Grappler primaryHoldItem;
    
    // States, counters, etc.
    private bool isGrounded, wasGrounded;
    private float lastJumpTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        InputSystem.actions.FindAction("Jump").performed += ctx => Jump();
        moveAction = InputSystem.actions.FindAction("Move");
        pointAction = InputSystem.actions.FindAction("Point");
        InputSystem.actions.FindAction("Primary Attack").performed += ctx =>
        {
            if (primaryHoldItem == null) return;
            Vector2 aimPoint = Camera.main.ScreenToWorldPoint(pointAction.ReadValue<Vector2>());
            primaryHoldItem.Fire(aimPoint);
        };

        if (PrimaryHold.childCount > 0) SetPrimaryHoldItem(PrimaryHold.GetChild(0).GetComponent<Grappler>());
    }

    private void Update()
    {
        Aim();
    }

    private void FixedUpdate()
    {
        if (rb.IsAwake())
        {
            wasGrounded = isGrounded;
            isGrounded = GroundChecks.Any(t => Physics2D.OverlapCircle(t.position, GroundCheckCircleRadius, GroundLayer));
        
            // if (wasGrounded != isGrounded) print("Now " + (isGrounded ? "grounded" : "not grounded"));
        
            if (!wasGrounded && isGrounded)  // Just landed
            {
                // To fix the 'stickiness' of landings, add an impulse relative to the previous x-velocity
                var landingImpulse = prevLinearVelocity.x * Mathf.Abs(prevLinearVelocity.y) * LandingImpulseMultiplier;
                rb.AddForceX(landingImpulse, ForceMode2D.Impulse);

                // Handle queued jump
                if (Time.time < lastJumpTime + MaxJumpQueueTime) Jump();
            }
        }
        
        Move();
        prevLinearVelocity = rb.linearVelocity;
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocityY = JumpSpeed * 2;
        }
        else
        {
            if (primaryHoldItem) primaryHoldItem.Cancel();
            
            // Queue the jump by recording the time it was attempted
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
    }

    private void Aim()
    {
        var pointInput = pointAction.ReadValue<Vector2>();
        var delta = Camera.main.ScreenToWorldPoint(pointInput) - PrimaryHold.position;
        var lookAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        PrimaryHold.localEulerAngles = new Vector3(0, 0, lookAngle);
    }

    private void SetPrimaryHoldItem(Grappler item)
    {
        item.transform.SetParent(PrimaryHold);
        primaryHoldItem = item;
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
