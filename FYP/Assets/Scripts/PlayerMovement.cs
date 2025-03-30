using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerMovement : NetworkBehaviour
{
    private float moveSpeed;
    public Transform orientation;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;
    public GameObject playerCharacter;

    //jumping
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool canJump = true;
    private KeyCode jumpKey;

    //check if player is on ground
    public float playerHeight;
    public LayerMask groundMask;
    private bool grounded;
    public float groundDrag;
    private Transform spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        moveSpeed = Globals.PlayerMoveSpeed;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        jumpKey = Globals.JumpKey;

        if (IsOwner) //set player self to not be seen by player camera
        {
            playerCharacter.layer = 7;

            foreach (Transform child in playerCharacter.transform)
            {
                child.gameObject.layer = 7;
            }
        }

        if (SceneLoader.Instance.SceneLoaded("Arena"))
        {
            spawnPoint = GameObject.Find("spawnPoint").transform;
            gameObject.transform.position = spawnPoint.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner || !MatchManager.Instance.matchActive.Value) //player cannot be controlled unless you are the owner and if the match isnt started
            return;

        //raycast from center of player to check if we are on ground
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);

        RegisterInput();

        if(grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        //prevent player from falling / glitching through floor
        //if (gameObject.transform.position.y < 0.8f)
          //  gameObject.transform.position = new Vector3(gameObject.transform.position.x, 0.8f, gameObject.transform.position.z);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        MovePlayer();
    }

    private void RegisterInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //jump
        if (Input.GetKey(jumpKey) && canJump && grounded)
        {
            canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);


        //limit speed
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
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        canJump = true;
    }
}
