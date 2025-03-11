using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Globals : MonoBehaviour
{
    public static float PlayerCamSensX = 400f;
    public static float PlayerCamSensY = 400f;
    public static float PlayerMoveSpeed = 7f;
    public static KeyCode JumpKey = KeyCode.Space; //jump bind, default space bar
    public static KeyCode Primary = KeyCode.Alpha1;
    public static KeyCode Secondary = KeyCode.Alpha2;
    public static KeyCode Melee = KeyCode.Alpha3;
    public static Color CrosshairColour = Color.blue;
}
