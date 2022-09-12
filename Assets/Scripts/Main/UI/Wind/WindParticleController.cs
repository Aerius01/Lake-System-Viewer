using UnityEngine;
using System;

public class WindParticleController : MonoBehaviour
{
    private ParticleSystem particles;

    public void GetSystem()
    {
        this.particles = this.gameObject.GetComponent<ParticleSystem>();
        if (!UserSettings.showWindWeather) this.gameObject.SetActive(false);
    }

    public void UpdateDirection(Vector2 unitVector, float windSpeed)
    {
        // Set speed and direction
        var vel = this.particles.velocityOverLifetime;
        float relativeSpeed = windSpeed / 25f;

        vel.xMultiplier = unitVector.x * relativeSpeed * 150f;
        vel.zMultiplier = unitVector.y * relativeSpeed * 150f;
    }
}
