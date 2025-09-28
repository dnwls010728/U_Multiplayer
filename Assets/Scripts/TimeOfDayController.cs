using Unity.Netcode;
using UnityEngine;

public class TimeOfDayController : NetworkBehaviour
{
    private NetworkVariable<float> _temp = new(writePerm: NetworkVariableWritePermission.Server);
}
