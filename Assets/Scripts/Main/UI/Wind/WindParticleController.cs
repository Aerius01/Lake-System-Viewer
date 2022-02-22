using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WindParticleController : MonoBehaviour
{
    private ParticleSystem particles;
    public float baseOffset = 150f;
    private float lifeToSpeed = 15f/20f;

    private void Start()
    {
        this.particles = this.gameObject.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (WindMain.instance.newData)
        {
            if (WindMain.instance.isNull)
            {
                particles.gameObject.SetActive(false);
            }
            else
            {
                particles.gameObject.SetActive(true);

                (float?, float?) windData = WindMain.instance.windData;
                double radDir = (double)(360f - windData.Item1) * Math.PI / 180f;
                double radDir2 = (double)(windData.Item1) * Math.PI / 180f;
                Vector2 unitVector = new Vector2((float)Math.Cos(radDir), (float)Math.Sin(radDir));

                // rotate sprite face
                var mainParticle = particles.main;
                mainParticle.startRotationZ = (float)radDir2 - (float)Math.PI;

                // set speed and direction
                var vel = particles.velocityOverLifetime;
                float relativeSpeed = (float)windData.Item2 / 25f;

                float speedX = -unitVector.x * relativeSpeed * 80f;
                float speedZ = -unitVector.y * relativeSpeed * 80f;
                float fullSpeed = (float)Math.Sqrt(Math.Pow(speedX, 2) + Math.Pow(speedZ, 2));

                vel.x = speedX;
                vel.z = speedZ;

                // Adjust lifetime
                mainParticle.startLifetime = fullSpeed * lifeToSpeed;

                

                // apply offset via ratio


                particles.Clear();
            }
        }
    }
}
