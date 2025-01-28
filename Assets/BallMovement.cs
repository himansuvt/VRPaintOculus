using Oculus.Interaction;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    [Header("References")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Rigidbody ballRigidbody;

    [Header("Settings")]
    public float throwForceMultiplier = 2.5f;
    public float twoHandAttachDistance = 1f;
    public float twoHandGrabWindow = 0.3f;
    public float releaseGracePeriod = 0.5f;

    private float leftTriggerTime = 0f;
    private float rightTriggerTime = 0f;
    private float releaseTime = -1f;

    private bool isHeld = false;
    private Transform activeHand;
    private Vector3 previousHandPosition;
    private Vector3 handVelocity;

    private Vector3 twoHandPreviousMidpoint;
    private Vector3 twoHandVelocity;

    private bool isTwoHanded = false;
    public RayInteractor interactor;
    void Update()
    {
        if (interactor.CollisionInfo == null)
            HandleInput();

    }

    void HandleInput()
    {
        bool isLeftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool isRightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        if (isLeftTriggerPressed)
            leftTriggerTime = Time.time;

        if (isRightTriggerPressed)
            rightTriggerTime = Time.time;

        if (!isHeld && Time.time - releaseTime > releaseGracePeriod)
        {
            if (isLeftTriggerPressed && !isRightTriggerPressed)
                AttachToHand(leftHandTransform);

            else if (isRightTriggerPressed && !isLeftTriggerPressed)
                AttachToHand(rightHandTransform);

            else if (isLeftTriggerPressed && isRightTriggerPressed &&
                     Mathf.Abs(leftTriggerTime - rightTriggerTime) <= twoHandGrabWindow &&
                     Vector3.Distance(leftHandTransform.position, rightHandTransform.position) <= twoHandAttachDistance)
            {
                AttachToTwoHands();
            }
        }

        if (isHeld)
        {
            if (isTwoHanded)
            {
                float handDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);

                if (handDistance > twoHandAttachDistance)
                {
                    ReleaseBall();
                    return;
                }
                if (!isLeftTriggerPressed || !isRightTriggerPressed)
                {
                    float releaseTimeDifference = Mathf.Abs(leftTriggerTime - rightTriggerTime);
                    if (releaseTimeDifference <= twoHandGrabWindow)
                    {
                        ThrowBall(twoHandVelocity);
                    }
                    else
                    {
                        ReleaseBall();
                    }
                }
                else
                {
                    UpdateTwoHandAttachment();
                    TrackTwoHandVelocity();
                }
            }
            else
            {
                if (activeHand == leftHandTransform && isRightTriggerPressed)
                {
                    float handDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);
                    if (handDistance <= twoHandAttachDistance)
                    {
                        AttachToTwoHands();
                    }
                }
                else if (activeHand == rightHandTransform && isLeftTriggerPressed)
                {
                    float handDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);
                    if (handDistance <= twoHandAttachDistance)
                    {
                        AttachToTwoHands();
                    }
                }

                if (activeHand == leftHandTransform && !isLeftTriggerPressed)
                    ThrowBall(handVelocity);
                else if (activeHand == rightHandTransform && !isRightTriggerPressed)
                    ThrowBall(handVelocity);
                else
                {
                    TrackHandVelocity(activeHand);
                }
            }
        }
    }

    void AttachToHand(Transform hand)
    {
        isHeld = true;
        isTwoHanded = false;
        activeHand = hand;

        ballRigidbody.isKinematic = true;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        transform.position = hand.position;
        transform.rotation = hand.rotation;

        transform.SetParent(hand);
        previousHandPosition = hand.position;
    }

    void AttachToTwoHands()
    {
        isHeld = true;
        isTwoHanded = true;

        ballRigidbody.isKinematic = true;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        Vector3 centerPosition = (leftHandTransform.position + rightHandTransform.position) / 2f;
        transform.position = centerPosition;

        transform.SetParent(null);

        twoHandPreviousMidpoint = centerPosition;
    }

    void UpdateTwoHandAttachment()
    {
        Vector3 centerPosition = (leftHandTransform.position + rightHandTransform.position) / 2f;
        transform.position = centerPosition;

        Quaternion averageRotation = Quaternion.Lerp(leftHandTransform.rotation, rightHandTransform.rotation, 0.5f);
        transform.rotation = averageRotation;
    }

    void TrackTwoHandVelocity()
    {
        Vector3 currentMidpoint = (leftHandTransform.position + rightHandTransform.position) / 2f;
        twoHandVelocity = (currentMidpoint - twoHandPreviousMidpoint) / Time.deltaTime;
        twoHandPreviousMidpoint = currentMidpoint;
    }

    void ReleaseBall()
    {
        isHeld = false;
        isTwoHanded = false;

        transform.SetParent(null);
        ballRigidbody.isKinematic = false;

        ballRigidbody.velocity = Vector3.zero;
        releaseTime = Time.time;
    }

    void ThrowBall(Vector3 velocity)
    {
        isHeld = false;
        isTwoHanded = false;

        transform.SetParent(null);
        ballRigidbody.isKinematic = false;

        ballRigidbody.velocity = velocity * throwForceMultiplier;
        ballRigidbody.angularVelocity = Random.insideUnitSphere * 2.0f;

        releaseTime = Time.time;
    }

    void TrackHandVelocity(Transform hand)
    {
        handVelocity = (hand.position - previousHandPosition) / Time.deltaTime;
        previousHandPosition = hand.position;
    }

    void OnDrawGizmos()
    {
        if (isHeld)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (isTwoHanded ? twoHandVelocity : handVelocity));
        }
    }
}
