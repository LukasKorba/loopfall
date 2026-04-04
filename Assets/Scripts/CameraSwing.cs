using UnityEngine;

public class CameraSwing : MonoBehaviour
{
    public Vector3 mDiff;
    public Camera mMainCamera;

    // Spring follow along camera forward (X axis)
    public Transform ballTransform;
    private float baseX;           // Camera's original X position
    private float springVelocity;  // Current spring velocity
    private float springOffset;    // Current offset from baseX

    // MANUAL PARAM: Spring tuning
    private float springStiffness = 8.0f;   // How fast camera catches up
    private float springDamping = 4.0f;     // Prevents oscillation
    private float idealBallOffset = 1.727f; // Ball is ~1.727 units ahead of camera at rest
    private float maxOffset = 0.8f;         // Clamp so camera doesn't overreact

    void Start()
    {
        baseX = transform.position.x;
        springOffset = 0f;
        springVelocity = 0f;
    }

    void Update()
    {
        Vector3 weaker = mDiff;

        transform.position -= weaker;
        // MANUAL PARAM: Camera swing rotation strength (original: mDiff.z * 10)
        transform.Rotate(0.0f, 0.0f, mDiff.z * 10.0f);

        mDiff -= weaker;

        // Orientation-based FOV
        if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
            mMainCamera.fieldOfView = 50.0f;
        else if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            mMainCamera.fieldOfView = 50.0f;
        else if (Input.deviceOrientation == DeviceOrientation.Portrait)
            mMainCamera.fieldOfView = 70.0f;
        else if (Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            mMainCamera.fieldOfView = 70.0f;
    }

    void LateUpdate()
    {
        if (ballTransform == null) return;

        // How far the ball has drifted from its ideal position relative to camera
        float ballX = ballTransform.position.x;
        float currentCameraX = baseX + springOffset;
        float actualOffset = ballX - currentCameraX;
        float error = actualOffset - idealBallOffset;

        // Damped spring: camera follows ball, not the other way around
        float springForce = springStiffness * error - springDamping * springVelocity;
        springVelocity += springForce * Time.deltaTime;
        springOffset += springVelocity * Time.deltaTime;

        // Clamp to prevent wild swings
        springOffset = Mathf.Clamp(springOffset, -maxOffset, maxOffset);

        // Apply only to X (forward axis)
        Vector3 pos = transform.position;
        pos.x = baseX + springOffset;
        transform.position = pos;
    }

    public void ResetSpring()
    {
        springOffset = 0f;
        springVelocity = 0f;
    }
}
