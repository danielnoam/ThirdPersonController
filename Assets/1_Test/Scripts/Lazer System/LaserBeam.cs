using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    [HideInInspector] public float totalDistance = 0f;
    [HideInInspector] public float maxTotalDistance = 100f;
    
    public Vector3 startPosition;
    public Vector3 endPosition;
    public Vector3 hitNormal;
    public LaserBeam prefab;
    public Vector3 Direction => (endPosition - startPosition).normalized;
    private LaserOpticalElementBase _laserOpticalElementBaseThatTheBeamHit;
    [HideInInspector] public LineRenderer _lineRenderer;

    public LaserOpticalElementBase LaserOpticalElementBaseThatTheBeamHit { 
        get => _laserOpticalElementBaseThatTheBeamHit; 
        set {
            if (_laserOpticalElementBaseThatTheBeamHit == value) {
                return;
            }
            else {
                if (_laserOpticalElementBaseThatTheBeamHit) {
                    _laserOpticalElementBaseThatTheBeamHit.UnregisterLaserBeam(this);
                }

                _laserOpticalElementBaseThatTheBeamHit = value;

                if (_laserOpticalElementBaseThatTheBeamHit) {
                    _laserOpticalElementBaseThatTheBeamHit.RegisterLaserBeam(this);
                }
            }
        }
    }

    private void Awake() {                                                                                                               
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
    }

    public void SetBeamProperties(float width, Color color, Material material) {
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
        _lineRenderer.material = material;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }

    public void Propagate(Vector3 startPosition, Vector3 direction) {
        // Remember the original totalDistance before we add this segment
        float originalDistance = totalDistance;
        
        // Calculate how much distance we have left
        float remainingDistance = maxTotalDistance - totalDistance;
        
        // Check if we've exceeded total beam distance
        if (remainingDistance <= 0) {
            // No more propagation, just make this a zero-length beam
            this.startPosition = startPosition;
            this.endPosition = startPosition;
            UpdateVisuals();
            return;
        }

        Vector3 endPosition = startPosition + direction * remainingDistance;
        Vector3 hitNormal = Vector3.zero;

        if (Physics.Raycast(startPosition, direction, out RaycastHit hit, remainingDistance)) {
            endPosition = hit.point;
            hitNormal = hit.normal;

            LaserOpticalElementBaseThatTheBeamHit = hit.collider.TryGetComponent(out LaserOpticalElementBase opticalElement) ? opticalElement : null;
        }
        else {
            LaserOpticalElementBaseThatTheBeamHit = null;
        }

        this.startPosition = startPosition;
        this.endPosition = endPosition;
        this.hitNormal = hitNormal;
        
        // Update total distance
        float segmentLength = Vector3.Distance(startPosition, endPosition);
        totalDistance += segmentLength;
        
        UpdateVisuals();

        if (LaserOpticalElementBaseThatTheBeamHit) {
            // Pass this beam to the optical element
            LaserOpticalElementBaseThatTheBeamHit.Propagate(this);
            
            // After propagation through optical elements, 
            // we need to ensure the next segment in the chain 
            // has the updated total distance
            if (totalDistance < originalDistance + segmentLength) {
                // If the totalDistance wasn't properly updated by the optical element,
                // we'll ensure it's at least the original distance plus this segment
                totalDistance = originalDistance + segmentLength;
            }
        }
    }

    private void UpdateVisuals() {
        _lineRenderer.SetPosition(0, startPosition);
        _lineRenderer.SetPosition(1, endPosition);
    }
}