using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    private float moveSpeed;
    public Transform orientation;
    private Rigidbody rb;
    public GameObject playerCharacter;

    // Jumping
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool canJump = true;
    private KeyCode jumpKey;

    // Ground check
    public float playerHeight;
    public LayerMask groundMask;
    private bool grounded;
    public float groundDrag = 5f;
    private Transform spawnPoint;

    // Networked input from client
    private Vector2 networkedMoveInput;
    private bool networkedJumpInput;
    private Quaternion networkedRotation;


    //client side prediction
    public struct PlayerInputState
    {
        public Vector2 moveInput;
        public bool jumpInput;
        public int tick;

        public PlayerInputState(Vector2 moveInput, bool jumpInput, int tick)
        {
            this.moveInput = moveInput;
            this.jumpInput = jumpInput;
            this.tick = tick;
        }
    }

    private List<PlayerInputState> inputBuffer = new List<PlayerInputState>();

    private int localTick;

    private Vector2 currentInput;
    private bool currentJumpInput;

    private Vector3 serverPosition;
    private Quaternion serverRotation;
    private int serverLastReceivedTick;


    void Start()
    {
        moveSpeed = Globals.PlayerMoveSpeed;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        jumpKey = Globals.JumpKey;

        if (IsOwner)
        {
            SetLayerRecursively(playerCharacter, 7); // Hide local player
        }
        else
        {
            SetLayerRecursively(playerCharacter, 0); // Show other players normally
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        localTick++;

        if (Input.GetKey(KeyCode.LeftShift))
            moveSpeed = Globals.PlayerMoveSpeed;
        else
            moveSpeed = Globals.PlayerSprintSpeed;

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool jumpInput = Input.GetKey(jumpKey);

        currentInput = moveInput;
        currentJumpInput = jumpInput;

        // Save input locally
        PlayerInputState inputState = new PlayerInputState(moveInput, jumpInput, localTick);
        inputBuffer.Add(inputState);

        // Immediately predict movement locally
        PredictMove(inputState);

        // Send input to server
        SendInputServerRpc(moveInput, jumpInput, localTick, orientation.rotation);
    }


    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
        orientation.rotation = networkedRotation;

        if (grounded)
            rb.drag = groundDrag; // e.g., 4f - 6f
        else
            rb.drag = 0f;

        MovePlayer();

        if (networkedJumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 moveInput, bool jumpInput, int tick, Quaternion rotation)
    {
        // Apply inputs server side
        ApplyServerMovement(moveInput, jumpInput, rotation);

        // After moving, send back authoritative state
        SendStateToClientClientRpc(transform.position, transform.rotation, tick);
    }



    private void MovePlayer()
    {
        Vector3 moveDirection = orientation.forward * networkedMoveInput.y + orientation.right * networkedMoveInput.x;

        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        // Limit horizontal speed
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }


    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        canJump = true;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void PredictMove(PlayerInputState inputState)
    {
        Vector3 moveDirection = orientation.forward * inputState.moveInput.y + orientation.right * inputState.moveInput.x;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if (inputState.jumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ApplyServerMovement(Vector2 moveInput, bool jumpInput, Quaternion rotation)
    {
        orientation.rotation = rotation;

        Vector3 moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if (jumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    [ClientRpc]
    private void SendStateToClientClientRpc(Vector3 pos, Quaternion rot, int serverTick)
    {
        if (!IsOwner)
            return;

        serverPosition = pos;
        serverRotation = rot;
        serverLastReceivedTick = serverTick;

        Reconcile();
    }

    private float correctionSpeed = 10.0f; // adjust this value to control smoothing

    private void Reconcile()
    {
        float distanceError = Vector3.Distance(transform.position, serverPosition);

        if (distanceError > 0.05f) // allow tiny tolerance
        {
           // Debug.Log($"[Reconcile] Correction needed: {distanceError}");

            // Instead of snapping, interpolate smoothly
            transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * correctionSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, serverRotation, Time.deltaTime * correctionSpeed);

            // Remove acknowledged inputs
            inputBuffer.RemoveAll(x => x.tick <= serverLastReceivedTick);
        }
    }



}
