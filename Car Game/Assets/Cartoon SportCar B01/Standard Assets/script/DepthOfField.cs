using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ModernDepthOfField : MonoBehaviour
{
    [Header("Focus Settings")]
    public Transform focusTarget;
    public float focalLength = 10f;
    public float focalSize = 0.05f;
    [Range(0f, 1f)] public float aperture = 0.5f;

    [Header("Blur Settings")]
    public float maxBlurSize = 2f;
    public bool highResolution = true;

    [Header("Resources")]
    public Shader dofShader;
    private Material dofMaterial;
    private Camera _camera;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnDisable()
    {
        if (dofMaterial) DestroyImmediate(dofMaterial);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (dofShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (dofMaterial == null)
            dofMaterial = new Material(dofShader);

        // Розрахунок дистанції фокусу
        float distance;
        if (focusTarget != null)
        {
            distance = _camera.WorldToViewportPoint(focusTarget.position).z / _camera.farClipPlane;
        }
        else
        {
            distance = focalLength / _camera.farClipPlane;
        }

        // Передаємо параметри в шейдер
        dofMaterial.SetVector("_CurveParams", new Vector4(1f, focalSize, (1f / (1f - aperture) - 1f), distance));
        dofMaterial.SetFloat("_MaxBlurSize", maxBlurSize);

        if (highResolution)
        {
            // Повнорозмірний прохід
            Graphics.Blit(source, destination, dofMaterial, 0);
        }
        else
        {
            // Оптимізований прохід через тимчасову текстуру (Downsampling)
            int rtW = source.width / 2;
            int rtH = source.height / 2;
            RenderTexture rtLow = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);

            Graphics.Blit(source, rtLow, dofMaterial, 0);
            Graphics.Blit(rtLow, destination);

            RenderTexture.ReleaseTemporary(rtLow);
        }
    }
}