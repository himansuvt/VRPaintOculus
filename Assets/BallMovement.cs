using UnityEngine;
using UnityEngine.EventSystems;

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

    private float leftTriggerTime = 0f;
    private float rightTriggerTime = 0f;

    private bool isHeld = false;
    private Transform activeHand;
    private Vector3 previousHandPosition;
    private Vector3 handVelocity;

    private Vector3 twoHandPreviousMidpoint;
    private Vector3 twoHandVelocity;

    private bool isTwoHanded = false;

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }

    }

    void HandleInput()
    {
        bool isLeftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool isRightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);

        if (isLeftTriggerPressed)
            leftTriggerTime = Time.time;

        if (isRightTriggerPressed)
            rightTriggerTime = Time.time;

        if (!isHeld)
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
                if (!isLeftTriggerPressed && !isRightTriggerPressed)
                {
                    ThrowBall(twoHandVelocity);
                }
                else if (!isLeftTriggerPressed)
                {
                    AttachToHand(rightHandTransform); // Switch to right hand
                }
                else if (!isRightTriggerPressed)
                {
                    AttachToHand(leftHandTransform); // Switch to left hand
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
                        AttachToTwoHands(); // Switch to two-handed mode
                    }
                }
                else if (activeHand == rightHandTransform && isLeftTriggerPressed)
                {
                    float handDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);
                    if (handDistance <= twoHandAttachDistance)
                    {
                        AttachToTwoHands(); // Switch to two-handed mode
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

        Debug.Log($"Tracking two-hand velocity: {twoHandVelocity}");
    }

    void ReleaseBall()
    {
        isHeld = false;
        isTwoHanded = false;

        transform.SetParent(null);
        ballRigidbody.isKinematic = false;

        ballRigidbody.velocity = Vector3.zero;
    }

    void ThrowBall(Vector3 velocity)
    {
        isHeld = false;
        isTwoHanded = false;

        transform.SetParent(null);
        ballRigidbody.isKinematic = false;

        ballRigidbody.velocity = velocity * throwForceMultiplier;
        ballRigidbody.angularVelocity = Random.insideUnitSphere * 2.0f;

        Debug.Log($"Throw velocity: {velocity}, Final applied force: {velocity * throwForceMultiplier}");
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
