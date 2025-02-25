using UnityEngine;
using System.Collections.Generic;

public class ProximityRevealer : MonoBehaviour
{
    // List of materials that will be affected by this revealer
    public List<Material> targetMaterials = new List<Material>();
    
    // Settings
    public float maxDistance = 2.0f;
    public float falloffSoftness = 0.5f;
    
    // Optional offset from object's position
    public Vector3 positionOffset = Vector3.zero;
    
    private void Start()
    {
        // Initialize materials (in case you want to set initial values)
        UpdateMaterials();
    }
    
    private void Update()
    {
        // Update position each frame
        UpdateMaterials();
    }
    
    // Update all target materials with current position and settings
    private void UpdateMaterials()
    {
        Vector3 position = transform.position + positionOffset;
        
        foreach (Material material in targetMaterials)
        {
            if (material != null)
            {
                material.SetVector("_ObjectPosition", position);
                material.SetFloat("_MaxDistance", maxDistance);
                material.SetFloat("_FalloffSoftness", falloffSoftness);
            }
        }
    }
    
    // Add a material at runtime
    public void AddMaterial(Material material)
    {
        if (material != null && !targetMaterials.Contains(material))
        {
            targetMaterials.Add(material);
            UpdateMaterials();
        }
    }
    
    // Remove a material at runtime
    public void RemoveMaterial(Material material)
    {
        if (material != null && targetMaterials.Contains(material))
        {
            targetMaterials.Remove(material);
        }
    }
}