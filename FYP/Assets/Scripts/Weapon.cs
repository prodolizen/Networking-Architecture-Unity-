using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public int playerPoints; // Points earned on round win to purchase weapons
    private Transform slot1;
    private Transform slot2;
    private Transform slot3;

    public Camera playerCamera;

    public Player playerRef;

    public enum WeaponID
    {
        BEAMER = 0,
        MINI = 1,
        KNIFE = 2,
        REBEL = 3,
        RIOT = 4,
        BLASTER = 5,
        PEPPER = 6,
        POKER = 7,
        MAXI = 8,
    }

    public struct WeaponInfo
    {
        public WeaponID ID;
        public GameObject obj;
        public Weapon script; // Store the actual script reference
        public float damage;
    }

    [SerializeField]
    private List<GameObject> Weapons;

    [SerializeField]
    private List<WeaponInfo> weaponTests;

    private WeaponInfo weapon1;
    private WeaponInfo weapon2;
    private WeaponInfo weapon3;

    private int lastWeapon1 = 999;
    private int lastWeapon2 = 999;
    private int lastWeapon3 = 999;

    public int test;

    public Weapon weaponsTest;

    public PEPPER peppertest;

    public RaycastHit target = default;

    void Start()
    {
        // Default weapons
        weapon1.ID = WeaponID.REBEL;
        weapon2.ID = WeaponID.MINI;
        weapon3.ID = WeaponID.PEPPER;

        slot1 = gameObject.transform.Find("Slot 1");
        slot2 = gameObject.transform.Find("Slot 2");
        slot3 = gameObject.transform.Find("Slot 3");

        playerRef = gameObject.transform.GetComponentInParent<Player>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        SpawnSelected();
        Equip();
        target = crosshairDetection();
        //weaponsTest = target.transform.Find("WeaponHolder").GetComponent<Weapon>();
        
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            ShootActiveWeapon();
        }
    }

    private void SpawnSelected()
    {
        if (Weapons[(int)weapon1.ID] != null)
        {
            if ((int)weapon1.ID != lastWeapon1)
            {
                Destroy(weapon1.obj);
                weapon1.obj = Instantiate(Weapons[(int)weapon1.ID], slot1);
                weapon1.script = weapon1.obj.GetComponent<Weapon>(); // Get the weapon script
                weapon1.obj.SetActive(false);
                lastWeapon1 = (int)weapon1.ID;
            }
        }

        if (Weapons[(int)weapon2.ID] != null)
        {
            if ((int)weapon2.ID != lastWeapon2)
            {
                Destroy(weapon2.obj);
                weapon2.obj = Instantiate(Weapons[(int)weapon2.ID], slot2);
                weapon2.script = weapon2.obj.GetComponent<Weapon>();
                weapon2.obj.SetActive(false);
                lastWeapon2 = (int)weapon2.ID;
            }
        }

        if (Weapons[(int)weapon3.ID] != null)
        {
            if ((int)weapon3.ID != lastWeapon3)
            {
                Destroy(weapon3.obj);
                weapon3.obj = Instantiate(Weapons[(int)weapon3.ID], slot3);
                weapon3.script = weapon3.obj.GetComponent<Weapon>();
                weapon3.obj.SetActive(false);
                lastWeapon3 = (int)weapon3.ID;
            }
        }
    }

    private void Equip()
    {
        if (Input.GetKey(Globals.Primary))
        {
            weapon2.obj.SetActive(false);
            weapon3.obj.SetActive(false);
            weapon1.obj.SetActive(true);
        }
        if (Input.GetKey(Globals.Secondary))
        {
            weapon1.obj.SetActive(false);
            weapon3.obj.SetActive(false);
            weapon2.obj.SetActive(true);
        }
        if (Input.GetKey(Globals.Melee))
        {
            weapon1.obj.SetActive(false);
            weapon2.obj.SetActive(false);
            weapon3.obj.SetActive(true);
        }
    }

    private void ShootActiveWeapon()
    {
        if (weapon1.obj.activeSelf && weapon1.script != null)
        {
            weapon1.script.Shoot(target);
        }
        else if (weapon2.obj.activeSelf && weapon2.script != null)
        {
            weapon2.script.Shoot(target);
        }
        else if (weapon3.obj.activeSelf && weapon3.script != null)
        {
            weapon3.script.Shoot(target);
        }

        //weapon1.script.Shoot(target);
    }

    public virtual void Shoot(RaycastHit target)
    {
        Debug.Log("Base Weapon Shoot");
    }

    private RaycastHit crosshairDetection()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log(hit.collider.gameObject.tag);

            return hit;
        }

        else
            return hit;
    }
}
