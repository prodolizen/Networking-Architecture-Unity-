using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    float sensX;
    float sensY;

    public Transform orientation;
    public Transform weaponHolder;

    float rotationX;
    float rotationY;

    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        sensX = Globals.PlayerCamSensX;
        sensY = Globals.PlayerCamSensY;
    }

    private void Update()
    {
        if (!IsOwner || !MatchManager.Instance.matchActive.Value)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        if (Cursor.visible == true)
            Cursor.visible = false;

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * -sensY;

        rotationY += mouseX;
        rotationX += mouseY;

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // rotate the camera up/down and left/right
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);

        // rotate player orientation only left/right
        orientation.rotation = Quaternion.Euler(0, rotationY, 0);

        // no more setting weaponHolder.rotation here!!
        // Weapon stays child of camera and just tilts locally if you want
    }


}
