using System.Linq;
using UnityEngine;

public class Temp : MonoBehaviour
{
    public SkinnedMeshRenderer cloth;   // 옷 SMR
    public Animator character;          // 캐릭터(본 소유)

    [ContextMenu("Rebind")]
    void Rebind()
    {
        var map = character.GetComponentsInChildren<Transform>(true)
            .ToDictionary(t => t.name, t => t);

        var bones = cloth.bones;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] && map.TryGetValue(bones[i].name, out var t)) bones[i] = t;

        var hips = character.GetBoneTransform(HumanBodyBones.Head) ?? character.transform;
        if (!map.TryGetValue(cloth.rootBone ? cloth.rootBone.name : hips.name, out var rb)) rb = hips;

        cloth.rootBone = rb;
        cloth.bones    = bones;

        // 편의상 캐릭터 밑으로
        cloth.transform.SetParent(character.transform, false);
        Debug.Log("Rebind complete.");
    }
}
