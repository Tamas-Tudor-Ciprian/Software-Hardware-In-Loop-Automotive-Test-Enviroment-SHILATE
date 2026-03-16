using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to build a complete primitive-placeholder car with WheelColliders,
/// a ground plane, and all required scripts wired up.
/// Access via menu: SHILATE → Build Car Scene.
/// </summary>
public static class CarBuilder
{
    [MenuItem("SHILATE/Build Car Scene")]
    static void BuildCarScene()
    {
        // ── Ground Plane ──
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(50f, 1f, 50f);

        // Tarmac-like physics material
        PhysicsMaterial groundMat = new PhysicsMaterial("Tarmac")
        {
            staticFriction = 0.8f,
            dynamicFriction = 0.6f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        ground.GetComponent<Collider>().material = groundMat;

        // ── Car Root ──
        // Spawn at Y=0.5 so WheelCollider rays reach ground immediately
        // (ray length = suspensionDistance + radius = 0.55, from Y=0.5 reaches Y=-0.05)
        // This avoids the free-fall → spring explosion on first contact.
        GameObject car = new GameObject("Car");
        car.transform.position = new Vector3(0f, 0.5f, 0f);

        Rigidbody rb = car.AddComponent<Rigidbody>();
        rb.mass = 1500f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // ── Car Body (visual) ──
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform);
        body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        body.transform.localScale = new Vector3(1.8f, 0.6f, 4.2f);
        Object.DestroyImmediate(body.GetComponent<Collider>()); // physics handled by WheelColliders

        // Cabin (upper box)
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(car.transform);
        cabin.transform.localPosition = new Vector3(0f, 0.85f, -0.2f);
        cabin.transform.localScale = new Vector3(1.5f, 0.5f, 2.0f);
        Object.DestroyImmediate(cabin.GetComponent<Collider>());

        // ── Wheel dimensions ──
        float wheelRadius = 0.35f;
        float suspensionDistance = 0.2f;
        float wheelX = 0.85f;
        float wheelFrontZ = 1.3f;
        float wheelRearZ = -1.3f;
        float wheelY = 0f; // relative to car root (WheelCollider handles suspension)

        // ── Create WheelColliders + Visual Wheels ──
        WheelCollider wcFL = CreateWheelCollider(car, "WheelCollider_FL", new Vector3(-wheelX, wheelY, wheelFrontZ), wheelRadius, suspensionDistance);
        WheelCollider wcFR = CreateWheelCollider(car, "WheelCollider_FR", new Vector3(wheelX, wheelY, wheelFrontZ), wheelRadius, suspensionDistance);
        WheelCollider wcRL = CreateWheelCollider(car, "WheelCollider_RL", new Vector3(-wheelX, wheelY, wheelRearZ), wheelRadius, suspensionDistance);
        WheelCollider wcRR = CreateWheelCollider(car, "WheelCollider_RR", new Vector3(wheelX, wheelY, wheelRearZ), wheelRadius, suspensionDistance);

        Transform meshFL = CreateWheelMesh(car, "WheelMesh_FL", new Vector3(-wheelX, wheelY, wheelFrontZ), wheelRadius);
        Transform meshFR = CreateWheelMesh(car, "WheelMesh_FR", new Vector3(wheelX, wheelY, wheelFrontZ), wheelRadius);
        Transform meshRL = CreateWheelMesh(car, "WheelMesh_RL", new Vector3(-wheelX, wheelY, wheelRearZ), wheelRadius);
        Transform meshRR = CreateWheelMesh(car, "WheelMesh_RR", new Vector3(wheelX, wheelY, wheelRearZ), wheelRadius);

        // ── VehicleController ──
        VehicleController vc = car.AddComponent<VehicleController>();
        vc.wheelFL = wcFL;
        vc.wheelFR = wcFR;
        vc.wheelRL = wcRL;
        vc.wheelRR = wcRR;
        vc.wheelMeshFL = meshFL;
        vc.wheelMeshFR = meshFR;
        vc.wheelMeshRL = meshRL;
        vc.wheelMeshRR = meshRR;

        // ── ManualDriveInput ──
        ManualDriveInput manual = car.AddComponent<ManualDriveInput>();
        manual.vehicle = vc;

        // ── SimulationRunner ──
        SimulationRunner runner = car.AddComponent<SimulationRunner>();
        runner.vehicle = vc;
        runner.autoStart = false; // manual by default until a scenario is assigned
        manual.simulationRunner = runner;

        // ── Find or create LedaBroker and wire VehicleTelemetryBridge ──
        LedaBroker broker = Object.FindAnyObjectByType<LedaBroker>();
        if (broker == null)
        {
            GameObject brokerGO = new GameObject("LedaBroker");
            broker = brokerGO.AddComponent<LedaBroker>();
        }

        VehicleTelemetryBridge bridge = car.AddComponent<VehicleTelemetryBridge>();
        bridge.vehicle = vc;
        bridge.broker = broker;

        // ── Camera ──
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow follow = mainCam.gameObject.AddComponent<CameraFollow>();
            follow.target = car.transform;
        }

        // ── Select the car so user sees it ──
        Selection.activeGameObject = car;
        Debug.Log("[CarBuilder] Car scene built. Press Play to drive with WASD or assign a DrivingScenario to SimulationRunner.");
    }

    static WheelCollider CreateWheelCollider(GameObject parent, string name, Vector3 localPos, float radius, float suspDist)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        WheelCollider wc = go.AddComponent<WheelCollider>();
        wc.radius = radius;
        wc.suspensionDistance = suspDist;

        JointSpring spring = wc.suspensionSpring;
        spring.spring = 35000f;
        spring.damper = 5500f;
        spring.targetPosition = 0.35f;
        wc.suspensionSpring = spring;

        wc.mass = 20f;

        // Semi-realistic friction curves
        WheelFrictionCurve fwd = wc.forwardFriction;
        fwd.extremumSlip = 0.4f;
        fwd.extremumValue = 1f;
        fwd.asymptoteSlip = 0.8f;
        fwd.asymptoteValue = 0.5f;
        fwd.stiffness = 1f;
        wc.forwardFriction = fwd;

        WheelFrictionCurve side = wc.sidewaysFriction;
        side.extremumSlip = 0.25f;
        side.extremumValue = 1f;
        side.asymptoteSlip = 0.5f;
        side.asymptoteValue = 0.75f;
        side.stiffness = 1f;
        wc.sidewaysFriction = side;

        return wc;
    }

    static Transform CreateWheelMesh(GameObject parent, string name, Vector3 localPos, float radius)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        // Cylinder default height=2, radius=0.5 → scale to match wheel radius
        float diameter = radius * 2f;
        go.transform.localScale = new Vector3(diameter, 0.15f, diameter);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Object.DestroyImmediate(go.GetComponent<Collider>()); // WheelCollider handles physics
        return go.transform;
    }
}
