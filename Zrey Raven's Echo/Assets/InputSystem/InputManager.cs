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
 
    public static event Action OnCounterPressed;
    public bool isAttackButtonPressed { get; private set; }
    public bool justReleasedAttack { get; private set; }
    public float attackButtonHeldTime { get; private set; }

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
        // --- THIS IS THE FIX ---
        // We now listen to all three events for the Attack action.
        inputActions.Player.Attack.started += OnAttackPressed;
       
        inputActions.Player.Attack.canceled += OnAttackReleased;
        // --- END OF FIX ---

        inputActions.Player.Counter.performed += OnCounterInput;
        inputActions.Player.Enable();
        Debug.Log("<color=cyan>InputManager: OnEnable() - NOW LISTENING FOR INPUTS.</color>");
    }

    private void OnDisable()
    {
        // --- THIS IS THE FIX ---
        inputActions.Player.Attack.started -= OnAttackPressed;
       
        inputActions.Player.Attack.canceled -= OnAttackReleased;
        // --- END OF FIX ---

        inputActions.Player.Counter.performed -= OnCounterInput;
        inputActions.Player.Disable();
        Debug.Log("<color=orange>InputManager: OnDisable() - STOPPED LISTENING.</color>");
    }
    private void Update()
    {
        // If the button is down, increment the timer.
        if (isAttackButtonPressed)
        {
            attackButtonHeldTime += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        // We only need to reset the "just released" flag.
        justReleasedAttack = false;
    }

    // --- MODIFIED Input Handlers ---
    private void OnAttackPressed(InputAction.CallbackContext context)
    {
        isAttackButtonPressed = true;
        attackButtonHeldTime = 0f;
    }

    private void OnAttackReleased(InputAction.CallbackContext context)
    {
        isAttackButtonPressed = false;
        justReleasedAttack = true;
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
