using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class BendingManager : MonoBehaviour
{
    [SerializeField] [Range(.005f, .1f)] private float _bengingAmount = .015f;
    
    private float _previousAmount;

    private void Awake()
    {
        if (Application.isPlaying)
            Shader.EnableKeyword("ENABLE_BENDING");
        else
            Shader.DisableKeyword("ENABLE_BENDING");
        
        UpdateBendingAmount();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void Update()
    {
        if (Mathf.Abs(_previousAmount - _bengingAmount) > Mathf.Epsilon)
            UpdateBendingAmount();
    }

    private void UpdateBendingAmount()
    {
        _previousAmount = _bengingAmount;
        Shader.SetGlobalFloat(Shader.PropertyToID("_BendingAmount"), _bengingAmount);
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        camera.cullingMatrix = Matrix4x4.Ortho(-99, 99, -99, 99, .001f, 99) * camera.worldToCameraMatrix;
    }

    private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        camera.ResetCullingMatrix();
    }
}