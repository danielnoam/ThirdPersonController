using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class LaserMirror : LaserOpticalElementBase
{
    private readonly List<LaserBeamPair> _laserBeamPairs = new List<LaserBeamPair>();

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
        
        Vector3 outgoingDirection = Vector3.Reflect(pair.incoming.Direction, pair.incoming.hitNormal);
        pair.outgoing.Propagate(pair.incoming.endPosition, outgoingDirection);
    }

    private LaserBeamPair GetPairFromIncomingBeam(LaserBeam laserBeam) => _laserBeamPairs.Find(x => x.incoming == laserBeam);
}