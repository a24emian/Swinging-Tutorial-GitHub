using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingingDone : MonoBehaviour
{
    [Header("References")]
    public LineRenderer lr;
    public Transform gunTip, cam, player;
    public LayerMask whatIsGrappleable;
    public PlayerMovementGrappling pm;

    private Grappling grappling;

    [Header("Swinging")]
    [SerializeField] private float maxSwingDistance = 25f;
    private Transform swingTarget; // Transform of the target object
    private Vector3 swingPoint;    // Initial swing point
    private SpringJoint joint;

    [Header("OdmGear")]
    public Transform orientation;
    public Rigidbody rb;
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;

    [Header("Prediction")]
    public float predictionSphereCastRadius = 1f;
    public Transform predictionPoint;
    private RaycastHit predictionHit;

    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse0;

    private Vector3 currentGrapplePosition;

    private void Start()
    {
        grappling = GetComponent<Grappling>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(swingKey) && !grappling.IsGrappling()) StartSwing();
        if (Input.GetKeyUp(swingKey)) StopSwing();

        CheckForSwingPoints();

        if (joint != null) OdmGearMovement();
    }

    private void LateUpdate()
    {
        DrawRope();
        UpdateSwingPoint(); // Dynamically update swing point position
    }

    private void CheckForSwingPoints()
    {
        if (joint != null || grappling.IsGrappling()) return;

        RaycastHit sphereCastHit;
        RaycastHit raycastHit;

        Vector3 camPosition = cam.position;
        Vector3 camForward = cam.forward;

        bool sphereHit = Physics.SphereCast(camPosition, predictionSphereCastRadius, camForward,
                                            out sphereCastHit, maxSwingDistance, whatIsGrappleable);
        bool rayHit = Physics.Raycast(camPosition, camForward,
                                       out raycastHit, maxSwingDistance, whatIsGrappleable);

        Vector3 realHitPoint = Vector3.zero;

        if (rayHit)
        {
            realHitPoint = raycastHit.point;
            predictionHit = raycastHit;
        }
        else if (sphereHit)
        {
            realHitPoint = sphereCastHit.point;
            predictionHit = sphereCastHit;
        }

        if (realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = Vector3.Lerp(predictionPoint.position, realHitPoint, Time.deltaTime * 10f);
        }
        else
        {
            predictionPoint.gameObject.SetActive(false);
            predictionHit = default;
        }
    }

    private void StartSwing()
    {
        if (predictionHit.collider == null)
        {
            Debug.LogWarning("No valid swing target.");
            return;
        }

        // Stop grappling before swinging
        if (grappling.IsGrappling()) grappling.StopGrapple();

        pm.ResetRestrictions();
        pm.swinging = true;

        swingTarget = predictionHit.collider.transform; // Attach to the hit object
        swingPoint = predictionHit.point;              // Initial point on the object

        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;
        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;

        lr.positionCount = 2;
        currentGrapplePosition = gunTip.position;

        Debug.Log("Swing started.");
    }

    public void StopSwing()
    {
        pm.swinging = false;
        lr.positionCount = 0;

        if (joint != null)
        {
            Destroy(joint);
        }

        swingTarget = null; // Reset the swing target
    }

    private void UpdateSwingPoint()
    {
        if (swingTarget != null && joint != null)
        {
            // Continuously update the connected anchor to match the target's position
            swingPoint = swingTarget.position; // Use the target's position
            joint.connectedAnchor = swingPoint;
        }
    }

    private void OdmGearMovement()
    {
        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);
        if (Input.GetKey(KeyCode.W)) rb.AddForce(orientation.forward * forwardThrustForce * Time.deltaTime);

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistance = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed * Time.deltaTime;
            joint.maxDistance = extendedDistance * 0.8f;
            joint.minDistance = extendedDistance * 0.25f;
        }
    }

    private void DrawRope()
    {
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }
}