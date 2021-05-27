using System.Collections.Generic;
using UnityEngine;
 
public class ParticleManager : MonoBehaviour
{
    // Replace these with whatever your data lookup is
 
    public int pointCount = 100;
 
    public float speed = 1f;
 
    public float radius = 1f;
 
    [HideInInspector]
    public List<Vector3> directions = new List<Vector3>();
 
    [HideInInspector]
    public List<Vector3> points = new List<Vector3>();
 
 
    // Keep this
 
    public ParticleSystem particleSystem;
 
    [HideInInspector]
    public ParticleSystem.Particle[] particles;
 
    private void InitializeData ()
    {
        for ( var i = 0; i < pointCount; i++ )
        {
            points.Add( Random.insideUnitSphere * radius );
        }
 
        for ( var i = 0; i < pointCount; i++ )
        {
            directions.Add( Random.onUnitSphere );
        }
    }
 
    private void InitializeParticles ()
    {
        particles = new ParticleSystem.Particle[pointCount];
 
        particleSystem.Emit( pointCount );
    }
 
    // Initialize stuff
    private void Start ()
    {
        InitializeData();
        InitializeParticles();
    }
 
    // Update points based on dataset at current time (to be replaced with however you do it)
    private void UpdateData ()
    {
        var n = points.Count;
 
        for ( var i = 0; i < n; i++ )
        {
            points[i] += directions[i] * Time.deltaTime * speed;
        }
    }
 
    // Move particles to latest point positions
    private void UpdateParticles ()
    {
        var n = points.Count;
 
        particleSystem.GetParticles( particles, n, 0 );
 
        for ( var i = 0; i < n; i++ )
        {
            particles[i].position = points[i];
            particles[i].remainingLifetime = 10f;
        }
 
        particleSystem.SetParticles( particles, n, 0 );
    }
 
    private void Update ()
    {
        UpdateData();
        UpdateParticles();
    }
}