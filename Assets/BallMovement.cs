using Oculus.Interaction;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BallMovement : MonoBehaviour
{
    [Header("References")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Rigidbody ballRigidbody;
    public Transform basketPosition;
    public Toggle biasToggle;
    public RayInteractor interactor;
    public ScoreManager scoreManager;

    [Header("Settings")]
    public float throwForceMultiplier = 2.5f;
    public float twoHandAttachDistance = 1f;
    public float twoHandGrabWindow = 0.3f;
    public float releaseGracePeriod = 0.5f;
    public bool successTriggered = false;
    public float waitTime = 2.5f;


    [Header("Bias Settings")]
    public bool isBiased = true;
    public float biasTolerance = 0.5f;
    public float correctionStrength = 0.2f;
    public float activationDistance = 5f;
    public float angleTolerance = 45f;

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
    private void Start()
    {
        if (biasToggle != null)
        {
            biasToggle.isOn = isBiased;
            biasToggle.onValueChanged.AddListener(ToggleBiasMode);
        }
    }
    void ToggleBiasMode(bool isOn)
    {
        isBiased = isOn;
        Debug.Log("Bias mode updated: " + (isBiased ? "Enabled" : "Disabled"));
    }
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
        if (!isHeld && isBiased)
        {
            Vector3 toBasket = basketPosition.position - transform.position;
            float distanceToBasket = toBasket.magnitude;
            float throwAngle = Vector3.Angle(ballRigidbody.velocity, toBasket);

            if (distanceToBasket < activationDistance && throwAngle <= angleTolerance)
            {
                ballRigidbody.velocity = AdjustVelocityForBias(
                    ballRigidbody.velocity,
                    transform.position,
                    basketPosition.position,
                    Time.deltaTime * correctionStrength
                );
            }
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        scoreManager.CheckGoal( other );
    }
    void AttachToHand(Transform hand)
    {
        StopAllCoroutines();
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
        StopAllCoroutines();
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

        scoreManager.ResetSequence();
    }

    void ThrowBall(Vector3 velocity)
    {
        isHeld = false;
        isTwoHanded = false;

        transform.SetParent(null);
        ballRigidbody.isKinematic = false;

        Vector3 predictedPoint = PredictLandingPoint(transform.position, velocity);

        if (isBiased && IsInBiasRegion(predictedPoint, basketPosition.position, biasTolerance))
        {
            velocity = AdjustVelocityForBias(velocity, transform.position, basketPosition.position, correctionStrength);
        }

        ballRigidbody.velocity = velocity;
        ballRigidbody.angularVelocity = Random.insideUnitSphere * 2.0f;

        releaseTime = Time.time;
        StartCoroutine(CheckOutcome());
    }


    IEnumerator CheckOutcome()
    {
        float elapsed = 0f;

        while (elapsed < waitTime)
        {
            if (successTriggered)
            {
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        scoreManager.HandleMiss();
    }



    void TrackHandVelocity(Transform hand)
    {
        handVelocity = (hand.position - previousHandPosition) / Time.deltaTime;
        previousHandPosition = hand.position;
    }

    Vector3 PredictLandingPoint(Vector3 startPosition, Vector3 initialVelocity)
    {
        Vector3 currentVelocity = initialVelocity;
        Vector3 position = startPosition;

        float timeStep = 0.02f;
        float g = Mathf.Abs(Physics.gravity.y);
        float maxSimulationTime = 5.0f;

        for (float t = 0; t < maxSimulationTime; t += timeStep)
        {
            currentVelocity.y -= g * timeStep;
            position += currentVelocity * timeStep;

            if (position.y <= 0.5f)
                break;
        }

        return position;
    }
    bool IsInBiasRegion(Vector3 predictedPoint, Vector3 basketPosition, float tolerance)
    {
        Vector3 adjustedPredictedPoint = new Vector3(predictedPoint.x, basketPosition.y, predictedPoint.z);

        return Vector3.Distance(adjustedPredictedPoint, basketPosition) <= tolerance;
    }

    Vector3 AdjustVelocityForBias(Vector3 velocity, Vector3 ballPosition, Vector3 basketPosition, float correctionStrength)
    {
        Vector3 midpoint = Vector3.Lerp(ballPosition, basketPosition, 0.8f) + Vector3.up * 2f;

        Vector3 toMidpoint = midpoint - ballPosition;
        Vector3 toBasket = basketPosition - ballPosition;

        Vector3 desiredVelocity = toMidpoint.normalized * velocity.magnitude;
        return Vector3.Lerp(velocity, desiredVelocity, correctionStrength);
    }


    void OnDrawGizmos()
    {
        Vector3 midpoint = Vector3.Lerp(transform.position, basketPosition.position, 0.5f) + Vector3.up * 2f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, midpoint);
        Gizmos.DrawLine(midpoint, basketPosition.position);
    }


}
