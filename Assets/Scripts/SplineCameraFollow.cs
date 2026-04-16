using UnityEngine;

/// <summary>
/// Camera follower for the spline track. Positions the camera behind the ball,
/// looking along the spline tangent. Replaces CameraSwing's fixed X-axis tracking
/// with spline-aware following.
///
/// The camera maintains a fixed offset behind the ball in the spline's local frame:
///   - Behind along tangent (looking forward down the track)
///   - Slightly above the track center (same vantage as torus camera)
///   - Lateral spring follows the ball's cross-section swing
/// </summary>
public class SplineCameraFollow : MonoBehaviour
{
    public SplineTrack splineTrack;
    public Transform ballTransform;
    public Camera cam;

    // ── OFFSET FROM BALL ─────────────────────────────────────
    // In the spline's local frame: tangent (forward), normal (up), binormal (side)

    // How far behind the ball the camera sits (along -tangent)
    float followDistance = 1.727f;

    // Height offset from track centerline (along normal)
    // Negative = below centerline but still above the ball (which sits at -0.9)
    // Matches torus camera: 0.695 above ball = centerline - 0.205
    float heightOffset = -0.205f;

    // ── SMOOTHING ────────────────────────────────────────────

    // Position smoothing (higher = snappier)
    float positionSmooth = 8f;

    // Rotation smoothing
    float rotationSmooth = 6f;

    // Lateral spring (follows ball swinging left/right)
    float lateralSpring = 6f;
    float lateralDamping = 4f;
    float lateralOffset;
    float lateralVelocity;
    float maxLateralOffset = 0.8f;

    // ── LOOK-AHEAD ───────────────────────────────────────────

    // Camera looks slightly ahead of the ball for a sense of speed
    float lookAheadDistance = 4f;

    // Track the ball's spline distance for smooth following
    float lastBallDist = 0f;
    bool firstFrame = true;

    void LateUpdate()
    {
        if (splineTrack == null || ballTransform == null) return;
        if (splineTrack.TotalLength < 1f) return;

        // Find ball's position on the spline
        float ballDist = splineTrack.FindClosestDistance(ballTransform.position);
        lastBallDist = ballDist;

        // Get spline frame at ball position
        Vector3 splinePos, tangent, normal, binormal;
        splineTrack.EvaluateFrame(ballDist, out splinePos, out tangent, out normal, out binormal);

        // Lateral offset: how far the ball is from the spline centerline
        Vector3 ballOffset = ballTransform.position - splinePos;
        float ballLateral = Vector3.Dot(ballOffset, binormal);

        // Spring-follow the lateral offset
        float lateralError = ballLateral - lateralOffset;
        float springForce = lateralSpring * lateralError - lateralDamping * lateralVelocity;
        lateralVelocity += springForce * Time.deltaTime;
        lateralOffset += lateralVelocity * Time.deltaTime;
        lateralOffset = Mathf.Clamp(lateralOffset, -maxLateralOffset, maxLateralOffset);

        // Target camera position: behind the ball along -tangent, above, with lateral tracking
        Vector3 targetPos = splinePos
            - tangent * followDistance
            + normal * heightOffset
            + binormal * lateralOffset;

        // Target look point: ahead of the ball
        float lookDist = Mathf.Min(ballDist + lookAheadDistance, splineTrack.TotalLength - 0.1f);
        Vector3 lookTarget = splineTrack.EvaluatePosition(lookDist);
        // Raise the look target slightly above track center
        Vector3 lookTangent, lookNormal, lookBinormal;
        Vector3 lookPos;
        splineTrack.EvaluateFrame(lookDist, out lookPos, out lookTangent, out lookNormal, out lookBinormal);
        lookTarget += lookNormal * 0.1f; // look slightly above center

        // First frame: snap immediately (no lerp from stale camera position)
        if (firstFrame)
        {
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, normal);
            firstFrame = false;
            return;
        }

        // Smooth position
        transform.position = Vector3.Lerp(transform.position, targetPos, positionSmooth * Time.deltaTime);

        // Smooth rotation — look along the track
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position, normal);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
    }

    /// <summary>Current ball distance on spline, for other systems to query.</summary>
    public float BallDistance { get { return lastBallDist; } }

    public void ResetSpring()
    {
        lateralOffset = 0f;
        lateralVelocity = 0f;
        firstFrame = true;
    }
}
