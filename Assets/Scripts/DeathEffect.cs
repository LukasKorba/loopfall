using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    private float shakeTimer = 0f;
    private float shakeDuration = 0.5f;
    private float shakeIntensity = 0.15f;
    private Vector3 shakeOffset = Vector3.zero;
    private bool shaking = false;
    private Camera cam;
    private ParticleSystem deathParticles;

    void Start()
    {
        cam = GetComponent<Camera>();
        CreateParticleSystem();
    }

    void CreateParticleSystem()
    {
        GameObject psObj = new GameObject("DeathParticles");
        psObj.transform.parent = transform;
        psObj.transform.localPosition = new Vector3(0, 0, 2.0f); // In front of camera

        deathParticles = psObj.AddComponent<ParticleSystem>();
        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = deathParticles.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 8.0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2.0f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.3f, 0.1f, 1f),
            new Color(1f, 0.6f, 0.2f, 1f)
        );

        var emission = deathParticles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 60)
        });

        var shape = deathParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var colorOverLifetime = deathParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = deathParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // Use Standard shader (already included in build)
        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Standard"));
        particleMat.color = new Color(1f, 0.4f, 0.15f);
        particleMat.EnableKeyword("_EMISSION");
        particleMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f));
        renderer.material = particleMat;

        deathParticles.Stop();
    }

    public void TriggerDeath(Vector3 ballWorldPos)
    {
        // Remove any existing shake offset first
        transform.position -= shakeOffset;
        shakeOffset = Vector3.zero;

        shaking = true;
        shakeTimer = shakeDuration;

        // Particles at ball position
        if (deathParticles != null)
        {
            deathParticles.transform.position = ballWorldPos;
            deathParticles.Clear();
            deathParticles.Play();
        }
    }

    public void ResetShake()
    {
        transform.position -= shakeOffset;
        shakeOffset = Vector3.zero;
        shaking = false;
        shakeTimer = 0f;
    }

    void LateUpdate()
    {
        if (shaking)
        {
            // Remove previous shake offset
            transform.position -= shakeOffset;

            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                shaking = false;
                shakeOffset = Vector3.zero;
            }
            else
            {
                float decay = shakeTimer / shakeDuration;
                shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity * decay,
                    Random.Range(-1f, 1f) * shakeIntensity * decay,
                    0
                );
                transform.position += shakeOffset;
            }
        }
    }

    public bool IsShaking() { return shaking; }
}
