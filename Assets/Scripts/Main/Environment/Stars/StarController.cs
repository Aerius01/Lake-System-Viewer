using UnityEngine;
using System.Collections.Generic;

public class StarController : MonoBehaviour
{
    // Inputs
    [SerializeField]
    private MoonController moonController;

    // Method variables
    private List<Transform> starSystems;

    private void Start()
    {
        starSystems = new List<Transform>();

        for (int i = 0; i < this.transform.childCount; i++)
        {
            starSystems.Add(this.transform.GetChild(i));
            starSystems[i].position = LocalMeshData.meshCenter;
            starSystems[i].GetComponent<ParticleSystem>().Clear();
        }
    }

    private void Update()
    {
        // Determine mode based on moon phase
        float mode = 
            (moonController.phase < 0.1 || moonController.phase > 0.9) ? 5 : 
            (moonController.phase < 0.2 || moonController.phase > 0.8) ? 4 :
            (moonController.phase < 0.3 || moonController.phase > 0.7) ? 3 :
            (moonController.phase < 0.4 || moonController.phase > 0.6) ? 1.5f : 0.3f
        ;

        for (int i = 0; i < starSystems.Count; i++)
        {
            ParticleSystem pSystem = starSystems[i].GetComponent<ParticleSystem>();

            // Control maximum particles by mode
            var pSystemMain = pSystem.main;
            pSystemMain.maxParticles = Mathf.RoundToInt(200f * mode);

            // Have stars mirror moon transparency
            Material currentMat = starSystems[i].GetComponent<ParticleSystemRenderer>().material;
            currentMat.SetFloat("Vector1_a794225ed2ac41ffb2b02b9016ce9ff5", moonController.moonAlpha);

            // Only render necessary particles
            if (pSystem.particleCount > pSystemMain.maxParticles)
            {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[pSystem.particleCount];
                pSystem.SetParticles(particles, pSystemMain.maxParticles);
            }
        }
    }
}
