using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using Unity.Burst.CompilerServices;
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
    private WeaponInfo activeWeapon;

    private int lastWeapon1 = 999;
    private int lastWeapon2 = 999;
    private int lastWeapon3 = 999;

    public int test;

    public Weapon weaponsTest;

    public PEPPER peppertest;

    public RaycastHit target = default;

    private Quaternion initialRotation3;
    private bool firstRun;

    private int _damage = 25;
    private int _headDamage = 100;

    void Start()
    {
        // Default weapons
        weapon1.ID = WeaponID.REBEL;
        weapon2.ID = WeaponID.MINI;
        weapon3.ID = WeaponID.KNIFE;

        slot1 = gameObject.transform.Find("Slot 1");
        slot2 = gameObject.transform.Find("Slot 2");
        slot3 = gameObject.transform.Find("Slot 3");

       // playerRef = gameObject.transform.GetComponentInParent<Player>();
        initialRotation3 = slot3.transform.localRotation;

       // SpawnSelected();
        firstRun = true;
    }

    void Update()
    {
        if (!IsOwner)
            return;
        if (playerRef == null)
        {
            playerRef = gameObject.transform.GetComponentInParent<Player>();
            Debug.Log("still not foun dplayer");
        }

        SpawnSelected();

        if (firstRun && MatchManager.Instance.matchActive.Value)
        {
            weapon1.obj.SetActive(true);
            firstRun = false;
        }
        Equip();
        target = crosshairDetection();
        //weaponsTest = target.transform.Find("WeaponHolder").GetComponent<Weapon>();

        if (Input.GetMouseButtonDown(0))
        {
            //var netObj = target.transform?.root.GetComponent<NetworkObject>();
            //ulong id = netObj != null ? netObj.NetworkObjectId : 0u;
            //Debug.Log($"[Client] Raycast hit “{target.collider?.gameObject.name}”, sending RPC with targetId={id}");
            //playerRef.DealDamageServerRpc(
            //    // use GetComponentInParent in case the collider is on a child
            //    target.collider?.GetComponentInParent<NetworkObject>()?.NetworkObjectId ?? 0u,
            //    _damage
            //);

            if (target.collider.gameObject.tag == "Head")
            {
                Debug.Log("shooting head");
                playerRef.DealDamageServerRpc(_headDamage);
            }

            else if (target.collider.gameObject.tag == "Body")
            {
                Debug.Log("shooting body");
                playerRef.DealDamageServerRpc(_damage);
            }
        }
    }

    //[ServerRpc(RequireOwnership = false)]
    //private void TryDealDamageServerRpc(ulong targetId, int damageAmount)
    //{
    //    Debug.Log($"[Server] Hit report: target={targetId}, dmg={damageAmount}");

    //    if (targetId == 0 ||
    //        !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var obj))
    //        return;

    //    var player = obj.GetComponent<Player>();
    //    if (player != null)
    //    {
    //        player.ApplyDamage(damageAmount);
    //    }
    //}

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
            activeWeapon = weapon1;
            _damage = 25;
        }
        if (Input.GetKey(Globals.Secondary))
        {
            weapon1.obj.SetActive(false);
            weapon3.obj.SetActive(false);
            weapon2.obj.SetActive(true);
            activeWeapon = weapon2;
            _damage = 10;
        }
        if (Input.GetKey(Globals.Melee))
        {
            weapon1.obj.SetActive(false);
            weapon2.obj.SetActive(false);
            weapon3.obj.SetActive(true);
            activeWeapon = weapon3;
        }
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
