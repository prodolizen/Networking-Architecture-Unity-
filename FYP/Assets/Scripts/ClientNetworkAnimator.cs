using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode.Components;

//will be used to sync animations to the server
public class ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; 
    }
}
