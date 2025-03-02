using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BoxCollider))]
public class LaserPortal : LaserOpticalElementBase {
    public Transform target;

    private readonly List<LaserBeamPair> _laserBeamPairs = new List<LaserBeamPair>();

    private BoxCollider _boxCollider;

    private void Awake() {
        _boxCollider = GetComponent<BoxCollider>();
    }

    public override void RegisterLaserBeam(LaserBeam laserBeam) {
        LaserBeam outgoingLaserBeam = GameObject.Instantiate(laserBeam.prefab, transform);
        
        // Copy beam properties (color, width, material)
        outgoingLaserBeam.SetBeamProperties(
            laserBeam._lineRenderer.startWidth, 
            laserBeam._lineRenderer.startColor, 
            laserBeam._lineRenderer.material
        );
        
        // Share the same maximum total distance
        outgoingLaserBeam.maxTotalDistance = laserBeam.maxTotalDistance;
        
        // Inherit the accumulated distance from the incoming beam
        outgoingLaserBeam.totalDistance = laserBeam.totalDistance;
        
        _laserBeamPairs.Add(new LaserBeamPair(laserBeam, outgoingLaserBeam));
    }
    
    public override void UnregisterLaserBeam(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);

        if (pair.outgoing.LaserOpticalElementBaseThatTheBeamHit != null) {
            pair.outgoing.LaserOpticalElementBaseThatTheBeamHit.UnregisterLaserBeam(pair.outgoing);
        }

        _laserBeamPairs.Remove(pair);
        GameObject.Destroy(pair.outgoing.gameObject);
    }
    
    public override void Propagate(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        // Update the outgoing beam's totalDistance to match the incoming beam
        // This ensures the accumulated distance is passed along
        pair.outgoing.totalDistance = pair.incoming.totalDistance;
        
        var localHitPosition = transform.InverseTransformPoint(pair.incoming.endPosition);

        Vector3 targetPosition = target.TransformPoint(localHitPosition);

        // Calculate the target direction
        Vector3 localDirection = transform.InverseTransformDirection(pair.incoming.Direction);
        Vector3 targetDirection = target.TransformDirection(localDirection);

        // We add a small offset to the target position to avoid the beam being stuck in the portal
        // This is dependent on size of collider
        targetPosition += targetDirection * _boxCollider.size.z;
        pair.outgoing.Propagate(targetPosition, targetDirection);
    }

    private LaserBeamPair GetPairFromIncomingBeam(LaserBeam laserBeam) => _laserBeamPairs.Find(x => x.incoming == laserBeam);
}