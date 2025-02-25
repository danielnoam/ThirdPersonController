using UnityEngine;

public class RevealTarget : MonoBehaviour
{
    private void Start()
    {
        // Find all ProximityRevealers in the scene
        ProximityRevealer[] revealers = FindObjectsOfType<ProximityRevealer>();
        
        // Get materials from this renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            foreach (var material in renderer.materials)
            {
                // Add this material to all revealers
                foreach (var revealer in revealers)
                {
                    revealer.AddMaterial(material);
                }
            }
        }
    }
}