using UnityEngine;
using System.Collections;

public class AttackSlash : MonoBehaviour
{
    [Header("Slash Visual")]
    [SerializeField] private float slashRange = 1.5f;
    [SerializeField] private float slashArcAngle = 90f; // Width of the arc in degrees
    [SerializeField] private int arcSegments = 20; // Smoothness of the arc
    [SerializeField] private float slashDuration = 0.2f;
    
    [Header("Colors")]
    [SerializeField] private Color slashStartColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color slashEndColor = new Color(1f, 1f, 1f, 0f);
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material slashMaterial;
    private GameObject slashObject;
    
    void Start()
    {
        // Create a child object for the slash
        slashObject = new GameObject("SlashVisual");
        slashObject.transform.SetParent(transform);
        slashObject.transform.localPosition = Vector3.zero;
        
        // Add mesh components
        meshFilter = slashObject.AddComponent<MeshFilter>();
        meshRenderer = slashObject.AddComponent<MeshRenderer>();
        
        // Create simple white texture
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // Create material using Sprites/Default or fallback
        slashMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
        slashMaterial.mainTexture = texture;
        slashMaterial.color = slashStartColor;
        
        meshRenderer.material = slashMaterial;
        meshRenderer.sortingOrder = 2;
        
        // Create mesh
        meshFilter.mesh = new Mesh();
        
        // Hide by default
        slashObject.SetActive(false);
    }
    
    public void PlaySlash(Vector2 direction)
    {
        StartCoroutine(SlashAnimation(direction));
    }
    
    private IEnumerator SlashAnimation(Vector2 direction)
    {
        // Calculate angle
        float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Show it
        slashObject.SetActive(true);
        
        float elapsed = 0f;
        float startAngle = centerAngle - 30f; // Start behind
        
        while (elapsed < slashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / slashDuration;
            
            // Fade out
            Color currentColor = Color.Lerp(slashStartColor, slashEndColor, progress);
            slashMaterial.color = currentColor;
            
            // Swing the arc forward (creates the "swing" motion)
            float currentAngle = Mathf.Lerp(startAngle, centerAngle + 30f, progress);
            
            // Draw the arc mesh
            UpdateArcMesh(currentAngle, 1f - progress * 0.2f); // Slight shrink at end
            
            yield return null;
        }
        
        // Hide it
        slashObject.SetActive(false);
    }
    
    private void UpdateArcMesh(float centerAngle, float scale = 1f)
    {
        Mesh mesh = meshFilter.mesh;
        mesh.Clear();
        
        float startAngle = centerAngle - (slashArcAngle / 2f);
        float endAngle = centerAngle + (slashArcAngle / 2f);
        float effectiveRange = slashRange * scale;
        
        // Create vertices - fan shape from player position
        Vector3[] vertices = new Vector3[arcSegments + 2];
        vertices[0] = Vector3.zero; // Center point at player
        
        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, (float)i / arcSegments);
            float rad = angle * Mathf.Deg2Rad;
            
            vertices[i + 1] = new Vector3(
                Mathf.Cos(rad) * effectiveRange,
                Mathf.Sin(rad) * effectiveRange,
                0f
            );
        }
        
        // Create triangles (fan from center)
        int[] triangles = new int[arcSegments * 3];
        for (int i = 0; i < arcSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
    
    public void SetSlashRange(float range)
    {
        slashRange = range;
    }
}