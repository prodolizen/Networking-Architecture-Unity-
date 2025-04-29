using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class REBEL : Weapon
{
    public WeaponID weapon;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Shoot(RaycastHit t)
    {
        Debug.Log("REBEL SHOOT");
        if (t.collider != null)
        {
            if (t.transform.CompareTag("Head"))
            {
                Debug.Log("sending to head ");
                playerRef.opponent.Damage();
            }

            if (t.transform.CompareTag("Body"))
            {
                playerRef.opponent.Damage();
            }
        }

    }
}
