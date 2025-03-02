using UnityEngine;
using UnityEngine.Serialization;

public class LaserSource : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField] private  Transform originTransform;
    [SerializeField] private  LaserBeam laserBeam;
    [SerializeField] private float beamLength = 100f;
    [SerializeField] private float beamWidth = 0.1f;
    [SerializeField] private Color beamColor = Color.red;
    [SerializeField] private Material beamMaterial;

    private void Start() {
        // Initialize the beam with the specified properties
        laserBeam.SetBeamProperties(beamWidth, beamColor, beamMaterial);
    }

    private void Update() {
        Vector3 startPosition = originTransform.position;
        Vector3 direction = originTransform.forward;

        // Reset accumulated distance and propagate
        laserBeam.totalDistance = 0f;
        laserBeam.maxTotalDistance = beamLength;
        laserBeam.Propagate(startPosition, direction);
    }
}