using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    float sensX;
    float sensY;

    public Transform orientation;

    float rotationX;
    float rotationY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        sensX = Globals.PlayerCamSensX;
        sensY = Globals.PlayerCamSensY;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * -sensY;

        rotationY += mouseX;
        rotationX += mouseY;

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        //set rotation of camera and player orientation
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        orientation.rotation = Quaternion.Euler(0, rotationY, 0);
    }
}
