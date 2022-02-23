using UnityEngine;
using System;

public class WindParticleController : MonoBehaviour
{
    private ParticleSystem particles;

    private void Start()
    {
        this.particles = this.gameObject.GetComponent<ParticleSystem>();
        if (!UserSettings.showWindWeather) this.gameObject.SetActive(false);
    }

    void Update()
    {
        if (WindWeatherMain.instance.newData)
        {
            if (!WindWeatherMain.instance.isNull)
            {
                // Collect and decompose data from WindMin
                (float?, float?) windData = WindWeatherMain.instance.windData;
                double radDir = (double)(360f - windData.Item1) * Math.PI / 180f;
                Vector2 unitVector = new Vector2((float)Math.Cos(radDir), (float)Math.Sin(radDir));

                // Set speed and direction
                var vel = particles.velocityOverLifetime;
                float relativeSpeed = (float)windData.Item2 / 25f;

                float speedX = -unitVector.x * relativeSpeed * 50f;
                float speedZ = -unitVector.y * relativeSpeed * 50f;

                vel.x = speedX;
                vel.z = speedZ;
            }
        }
    }
}
