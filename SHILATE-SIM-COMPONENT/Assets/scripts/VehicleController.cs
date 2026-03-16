using UnityEngine;

/// <summary>
/// Semi-realistic vehicle physics using Unity WheelColliders.
/// RWD drivetrain — motor torque on rear wheels, steering on front wheels.
/// Inputs are set externally by SimulationRunner or ManualDriveInput.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    [Header("Wheel Meshes (visual)")]
    public Transform wheelMeshFL;
    public Transform wheelMeshFR;
    public Transform wheelMeshRL;
    public Transform wheelMeshRR;

    [Header("Drivetrain")]
    [Tooltip("Maximum motor torque applied to rear wheels (Nm)")]
    public float maxMotorTorque = 400f;

    [Tooltip("Maximum steering angle for front wheels (degrees)")]
    public float maxSteerAngle = 35f;

    [Tooltip("Maximum brake torque applied to all wheels (Nm)")]
    public float maxBrakeTorque = 3000f;

    [Header("RPM Estimation")]
    [Tooltip("Final drive ratio for RPM calculation")]
    public float finalDriveRatio = 3.5f;

    [Tooltip("Idle RPM when stationary")]
    public float idleRPM = 800f;

    [Tooltip("Maximum engine RPM")]
    public float maxEngineRPM = 7000f;

    [Header("Center of Mass")]
    [Tooltip("Local center of mass position for stability (lower = more stable)")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.2f, 0.15f);

    // ─── Inputs (written by input scripts or SimulationRunner) ───

    /// <summary>Throttle input: 0 (none) to 1 (full throttle).</summary>
    [HideInInspector] public float ThrottleInput;

    /// <summary>Steering input: -1 (full left) to 1 (full right).</summary>
    [HideInInspector] public float SteerInput;

    /// <summary>Brake input: 0 (none) to 1 (full brake).</summary>
    [HideInInspector] public float BrakeInput;

    // ─── Read-only telemetry ───

    /// <summary>Current speed in km/h (derived from Rigidbody velocity).</summary>
    public float CurrentSpeed { get; private set; }

    /// <summary>Estimated engine RPM (derived from rear wheel angular velocity).</summary>
    public float CurrentRPM { get; private set; }

    /// <summary>Current front wheel steering angle in degrees.</summary>
    public float CurrentSteerAngle { get; private set; }

    Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = centerOfMassOffset;
    }

    void FixedUpdate()
    {
        ApplySteering();
        ApplyMotor();
        ApplyBrakes();
        UpdateTelemetry();
    }

    void LateUpdate()
    {
        SyncWheelMeshes();
    }

    void ApplySteering()
    {
        CurrentSteerAngle = SteerInput * maxSteerAngle;
        wheelFL.steerAngle = CurrentSteerAngle;
        wheelFR.steerAngle = CurrentSteerAngle;
    }

    void ApplyMotor()
    {
        float torque = ThrottleInput * maxMotorTorque;
        wheelRL.motorTorque = torque;
        wheelRR.motorTorque = torque;
    }

    void ApplyBrakes()
    {
        float brakeTorque = BrakeInput * maxBrakeTorque;
        wheelFL.brakeTorque = brakeTorque;
        wheelFR.brakeTorque = brakeTorque;
        wheelRL.brakeTorque = brakeTorque;
        wheelRR.brakeTorque = brakeTorque;
    }

    void UpdateTelemetry()
    {
        // Speed from Rigidbody velocity (m/s → km/h)
        CurrentSpeed = _rb.linearVelocity.magnitude * 3.6f;

        // RPM from average rear wheel angular velocity
        float avgWheelRPM = (Mathf.Abs(wheelRL.rpm) + Mathf.Abs(wheelRR.rpm)) * 0.5f;
        float engineRPM = avgWheelRPM * finalDriveRatio;
        CurrentRPM = Mathf.Clamp(Mathf.Max(engineRPM, idleRPM), idleRPM, maxEngineRPM);
    }

    void SyncWheelMeshes()
    {
        SyncWheel(wheelFL, wheelMeshFL);
        SyncWheel(wheelFR, wheelMeshFR);
        SyncWheel(wheelRL, wheelMeshRL);
        SyncWheel(wheelRR, wheelMeshRR);
    }

    void SyncWheel(WheelCollider collider, Transform mesh)
    {
        if (collider == null || mesh == null) return;
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    /// <summary>Reset all inputs to zero (stop the car).</summary>
    public void ResetInputs()
    {
        ThrottleInput = 0f;
        SteerInput = 0f;
        BrakeInput = 0f;
    }
}
