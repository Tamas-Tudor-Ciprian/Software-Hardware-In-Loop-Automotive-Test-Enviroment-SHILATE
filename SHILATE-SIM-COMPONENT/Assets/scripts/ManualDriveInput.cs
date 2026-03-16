using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the Input System "Player/Move" action and writes to VehicleController.
/// Vertical axis → throttle (positive) / brake (negative).
/// Horizontal axis → steering.
/// Disable this component when SimulationRunner is driving the car.
/// </summary>
public class ManualDriveInput : MonoBehaviour
{
    [Tooltip("The vehicle to control")]
    public VehicleController vehicle;

    [Tooltip("Optional — if assigned and running, manual input is suppressed")]
    public SimulationRunner simulationRunner;

    InputAction _moveAction;

    void OnEnable()
    {
        var map = InputSystem.actions?.FindActionMap("Player");
        _moveAction = map?.FindAction("Move");
        _moveAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
    }

    void Update()
    {
        if (vehicle == null) return;

        // Suppress manual input while a scripted scenario is active
        if (simulationRunner != null && simulationRunner.IsRunning) return;

        Vector2 input = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        vehicle.SteerInput = input.x;

        if (input.y >= 0f)
        {
            vehicle.ThrottleInput = input.y;
            vehicle.BrakeInput = 0f;
        }
        else
        {
            vehicle.ThrottleInput = 0f;
            vehicle.BrakeInput = -input.y;
        }
    }
}
