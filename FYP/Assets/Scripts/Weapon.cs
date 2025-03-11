using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public int playerPoints; //points earned on round win to purchase weapons
    public Transform slot1;
    public Transform slot2;
    public Transform slot3;
    public enum WeaponID
    {
        Beamer = 0,
        Glock = 1,
        Knife = 2,
    }

    public struct WeaponInfo
    {
        public WeaponID ID;
        public GameObject obj;
    }

    [SerializeField]
    private List<GameObject> Weapons;

    private WeaponInfo weapon1;
    private WeaponInfo weapon2;
    private WeaponInfo weapon3;

    private int lastWeapon1 = 999;
    private int lastWeapon2 = 999;
    private int lastWeapon3 = 999;

    public int test;
    // Start is called before the first frame update
    void Start()
    {
        //default weapons
        weapon1.ID = WeaponID.Beamer;
        weapon2.ID = WeaponID.Glock;
        weapon3.ID = WeaponID.Knife;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
            return;
        weapon1.ID = (WeaponID)test;
        SpawnSelected();
        Equip();
    }

    private void SpawnSelected() //spawn the correct weapons to corresponing slots
    {
        if (Weapons[(int)weapon1.ID] != null)
        {
            if ((int)weapon1.ID != lastWeapon1)
            {
                Destroy(weapon1.obj);
                weapon1.obj = Instantiate(Weapons[(int)weapon1.ID], slot1);
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
                weapon3.obj.SetActive(false);
                lastWeapon3 = (int)weapon3.ID;
            }
        }
    }

    private void Equip() //equip the weapon to players hand
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
}
