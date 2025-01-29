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
    public float releaseForceThreshold = 10f;
    public float waitTime = 2.5f;
    

    [Header("Bias Settings")]
    public bool isBiased = true;
    public float biasTolerance = 0.5f;
    public float correctionStrength = 0.2f;
    public float activationDistance = 5f;
    public float angleTolerance = 45f;
    [HideInInspector]public bool successTriggered = false;
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
        float handDistance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);

        if (isLeftTriggerPressed && !isRightTriggerPressed)
                AttachToHand(leftHandTransform);
            else if (isRightTriggerPressed && !isLeftTriggerPressed)
                AttachToHand(rightHandTransform);
            else if (isLeftTriggerPressed && isRightTriggerPressed)
                AttachToTwoHands();
        

        if (isHeld)
        {
            if (isTwoHanded)
            {
                UpdateTwoHandAttachment();
                TrackTwoHandVelocity();
               

                if (handDistance > twoHandAttachDistance)
                {
                    ReleaseBall();
                    return;
                }
                if (twoHandVelocity.magnitude > releaseForceThreshold)
                {
                    ThrowBall(twoHandVelocity * throwForceMultiplier);
                }
            }
            else
            {
                TrackHandVelocity(activeHand);

                if (handVelocity.magnitude > releaseForceThreshold)
                {
                    ThrowBall(handVelocity * throwForceMultiplier);
                }
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        scoreManager.CheckGoal(other);
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