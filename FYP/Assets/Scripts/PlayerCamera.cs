using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    private float sensX;
    private float sensY;

    public Transform orientation;
    public Transform weaponHolder;

    private float rotationX;
    private float rotationY;

    private Vector2 currentLookInput;

    private void Start()
    {
        sensX = Globals.PlayerCamSensX;
        sensY = Globals.PlayerCamSensY;

        Camera cam = GetComponent<Camera>();

        if (IsOwner)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;

            if (cam != null)
                cam.enabled = true;
        }
        else
        {
            if (cam != null)
                cam.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner || MatchManager.Instance == null || !MatchManager.Instance.matchActive.Value)
            return;

        //if (Cursor.lockState != CursorLockMode.Locked)
        //    Cursor.lockState = CursorLockMode.Locked;

        //if (Cursor.visible == true)
        //    Cursor.visible = false;

        sensX = Globals.PlayerCamSensX;
        sensY = Globals.PlayerCamSensY;

        HandleInput();
    }

    private void LateUpdate()
    {
        if (!IsOwner || MatchManager.Instance == null || !MatchManager.Instance.matchActive.Value)
            return;

        HandleLook();
    }

    private void HandleInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        currentLookInput = new Vector2(mouseX, mouseY);
    }

    private void HandleLook()
    {
        rotationY += currentLookInput.x;
        rotationX -= currentLookInput.y; // minus to invert vertical look

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        orientation.rotation = Quaternion.Euler(0, rotationY, 0);
    }
}
