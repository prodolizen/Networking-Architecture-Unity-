using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private float moveSpeed;
    public Transform orientation;
    private Rigidbody rb;
    public GameObject playerCharacter;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool canJump = true;
    private KeyCode jumpKey;

    public float playerHeight;
    public LayerMask groundMask;
    private bool grounded;
    public float groundDrag = 5f;

    private Vector2 currentInput;
    private bool currentJumpInput;
    private int localTick;

    private Dictionary<ulong, PlayerInputState> serverInputBuffer = new Dictionary<ulong, PlayerInputState>();

    private Vector3 serverPosition;
    private Quaternion serverRotation;
    private int serverLastReceivedTick;

    private List<PlayerInputState> inputBuffer = new List<PlayerInputState>();

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

    private Vector2 networkedMoveInput;
    private bool networkedJumpInput;
    private Quaternion networkedRotation;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Vector3 spawnPosition = GetSpawnPosition(OwnerClientId);
            transform.position = spawnPosition;
            serverPosition = spawnPosition;   // <- ADD THIS LINE
            serverRotation = transform.rotation;  // <- And this
            SetSpawnClientRpc(spawnPosition);
        }
    }


    [ClientRpc]
    private void SetSpawnClientRpc(Vector3 spawnPos)
    {
        if (!IsServer)
            transform.position = spawnPos;
    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        GameObject spawnPoint = GameObject.Find($"spawnPoint_{clientId + 1}");
        if (spawnPoint != null)
            return spawnPoint.transform.position;
        else
            return new Vector3(5f * clientId, 1, 0); // Fallback
    }

    private void Start()
    {
        moveSpeed = Globals.PlayerMoveSpeed;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        jumpKey = Globals.JumpKey;

        if (IsOwner)
        {
            SetLayerRecursively(playerCharacter, 7);
            rb.isKinematic = false;
            Debug.Log($"[Start] Player {OwnerClientId} owns this object (Local Player)");
        }
        else
        {
            SetLayerRecursively(playerCharacter, 0);
            if (IsServer)
            {
                rb.isKinematic = false;
            }
            else
            {
                rb.isKinematic = true;
            }
            Debug.Log($"[Start] Player {OwnerClientId} is NOT owned by us (Remote Player)");
        }
    }

    private void Update()
    {
        if (!IsOwner || MatchManager.Instance == null || !MatchManager.Instance.matchActive.Value)
            return;

        localTick++;

        moveSpeed = Input.GetKey(KeyCode.LeftShift) ? Globals.PlayerSprintSpeed : Globals.PlayerMoveSpeed;

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool jumpInput = Input.GetKey(jumpKey);

        currentInput = moveInput;
        currentJumpInput = jumpInput;

        PlayerInputState inputState = new PlayerInputState(moveInput, jumpInput, localTick);
        inputBuffer.Add(inputState);

        SendInputServerRpc(moveInput, jumpInput, localTick, orientation.rotation);
    }

    private void FixedUpdate()
    {
        if (MatchManager.Instance == null || !MatchManager.Instance.matchActive.Value)
            return;

        if (IsOwner)
        {
            UpdateGrounded();
            PredictMove(new PlayerInputState(currentInput, currentJumpInput, localTick));
        }

        if (IsServer)
        {
            UpdateGrounded();
            orientation.rotation = networkedRotation;

            //// ONLY apply server movement if the player sent input
            //if (networkedMoveInput != Vector2.zero || networkedJumpInput)
            //{
            //    ApplyMovement(networkedMoveInput, networkedJumpInput);
            //}
        }
        else if (!IsOwner)
        {
            // Remote players interpolate
            transform.position = Vector3.Lerp(transform.position, serverPosition, Time.fixedDeltaTime * 100f);
            transform.rotation = Quaternion.Lerp(transform.rotation, serverRotation, Time.fixedDeltaTime * 10f);
        }
    }



    private void UpdateGrounded()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
        rb.drag = grounded ? groundDrag : 0f;
    }

    private void PredictMove(PlayerInputState inputState)
    {
        Vector3 moveDirection = orientation.forward * inputState.moveInput.y + orientation.right * inputState.moveInput.x;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        ClampVelocity();

        if (inputState.jumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

       // Debug.Log($"[PredictMove] Client {OwnerClientId} local position: {transform.position}");
    }

    private void ApplyMovement(Vector2 moveInput, bool jumpInput)
    {
        Vector3 moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        ClampVelocity();

        if (jumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ClampVelocity()
    {
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

    [ServerRpc(RequireOwnership = false)]
    private void SendInputServerRpc(Vector2 moveInput, bool jumpInput, int tick, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            Debug.LogWarning($"[ServerRpc] Wrong client tried to move object! Sender={rpcParams.Receive.SenderClientId} Owner={OwnerClientId}");
            return;
        }

        // Update orientation immediately
        orientation.rotation = rotation;

        // Move the player right now using the input
        ServerMove(moveInput, jumpInput);

        // Send corrected position back to ONLY the owner client
        SendStateToClientClientRpc(transform.position, transform.rotation, tick, OwnerClientId);
    }


    [ClientRpc]
    private void SendStateToClientClientRpc(Vector3 pos, Quaternion rot, int serverTick, ulong clientId)
    {
        if (!IsOwner || NetworkManager.Singleton.LocalClientId != clientId)
            return;

     //   Debug.Log($"[ClientRpc] Local Client {NetworkManager.Singleton.LocalClientId} applying server position {pos}");

        serverPosition = pos;
        serverRotation = rot;
        serverLastReceivedTick = serverTick;
    }

    private void ServerMove(Vector2 moveInput, bool jumpInput)
    {
        UpdateGrounded();

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;

            if (grounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            else
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

            ClampVelocity();
        }

        if (jumpInput && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

       // Debug.Log($"[ServerMove] Moving player {OwnerClientId} to {transform.position}");
    }


}
