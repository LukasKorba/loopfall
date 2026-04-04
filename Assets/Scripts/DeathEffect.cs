using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    private float shakeTimer = 0f;
    private float shakeDuration = 0.5f;
    private float shakeIntensity = 0.15f;
    private Vector3 shakeOffset = Vector3.zero;
    private bool shaking = false;

    public void TriggerDeath(Vector3 ballWorldPos)
    {
        // Remove any existing shake offset first
        transform.position -= shakeOffset;
        shakeOffset = Vector3.zero;

        shaking = true;
        shakeTimer = shakeDuration;

        // Dual shockwave rings on the grid
        Shader.SetGlobalFloat("_DeathPulseTime", Time.time);
        Shader.SetGlobalVector("_DeathPulsePos", ballWorldPos);
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
