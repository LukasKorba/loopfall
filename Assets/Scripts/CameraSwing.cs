using UnityEngine;

public class CameraSwing : MonoBehaviour
{
    public Vector3 mDiff;
    public Camera mMainCamera;

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
}
