using Unity.Netcode;
using UnityEngine;

public class WorldState : NetworkBehaviour
{
    private readonly NetworkVariable<int> _score = new(writePerm: NetworkVariableWritePermission.Server);
    
    public override void OnNetworkSpawn()
    {
        SessionManager.Instance.WorldState = this;
    }
}
