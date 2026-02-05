using UnityEngine;
using UnityEngine.InputSystem;
using System;
public class InputManager : MonoBehaviour
{
    // --- The Singleton Pattern ---
    public static InputManager Instance { get; private set; }
    // ---

    // --- Input Actions ---
    private InputSystem_Actions inputActions;
    
    public KnightAI knightAI; // Reference to the KnightAI script to call its methods directly.

    // --- Public State Flags ---
    // These flags are true for ONLY ONE FRAME when the input is pressed.
    public bool justPressedAttack { get; private set; }
    public static event Action OnCounterPressed;

    private void Awake()
    {
        // --- Singleton Implementation ---
        // If an Instance already exists and it's not this one, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            // Otherwise, set the instance to this one and make it indestructible.
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        // ---
         knightAI = FindObjectOfType<KnightAI>(); // Find the KnightAI in the scene and store a reference to it.
        // --- Input Setup ---
        inputActions = new InputSystem_Actions();
        // ---
    }

    private void OnEnable()
    {
        // Subscribe to events and enable the action map.
        inputActions.Player.Attack.performed += OnAttackInput;
        inputActions.Player.Counter.performed += OnCounterInput;
        inputActions.Player.Enable();
        Debug.Log("<color=cyan>InputManager: OnEnable() - NOW LISTENING FOR INPUTS.</color>");
    }

    private void OnDisable()
    {
        // Unsubscribe and disable to prevent memory leaks.
        inputActions.Player.Attack.performed -= OnAttackInput;
        inputActions.Player.Counter.performed -= OnCounterInput;
        inputActions.Player.Disable();
        Debug.Log("<color=orange>InputManager: OnDisable() - STOPPED LISTENING.</color>");
    }

    // This method runs AFTER all Update() methods have run.
    private void LateUpdate()
    {
        justPressedAttack = false;

       
    }

    // --- Input Handlers ---
    private void OnAttackInput(InputAction.CallbackContext context)
    {
        justPressedAttack = true;
    }

    private void OnCounterInput(InputAction.CallbackContext context)
    {
        // --- THIS IS THE FIX ---
        // We no longer set a flag. We FIRE the event.
        // The '?' checks if anyone is listening before firing.
        OnCounterPressed?.Invoke();
        if (knightAI.counterPromptUI != null) knightAI.counterPromptUI.SetActive(false);
        // --- END OF FIX ---

        Debug.LogWarning("!!! InputManager FIRED OnCounterPressed EVENT !!!");
    }
}
