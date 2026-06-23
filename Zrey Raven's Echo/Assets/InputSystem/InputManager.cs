using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SceneManagement;
public class InputManager : MonoBehaviour
{
    // --- The Singleton Pattern ---
    public static InputManager Instance { get; private set; }
    // ---
   
    // --- Input Actions ---
    public static InputSystem_Actions inputActions;

    public KnightAI knightAI; // Reference to the KnightAI script to call its methods directly.
    public ZreyAttacks playerAttacks; 

  
    public bool isAttackButtonPressed { get; private set; }
    public bool justReleasedAttack { get; private set; }
    public float attackButtonHeldTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // --- THIS IS THE FIX ---
        // Instead of having the logic here, we just call our new, reusable method.
        ScanSceneForReferences();
        // ---
        knightAI = FindObjectOfType<KnightAI>(); // Find the KnightAI in the scene and store a reference to it.
                                                 // --- Input Setup ---
        inputActions = ZreyMovements.inputActions;
        // ---
    }
    private void SubscribeToEvents()
    {
        // Failsafe: If for some reason inputActions is null, do nothing.
        if (inputActions == null)
        {
            Debug.LogError("InputManager tried to subscribe to events, but inputActions was NULL!");
            return;
        }

        Debug.Log("<color=lime>InputManager: Subscribing to events...</color>");
        // Unsubscribe first to prevent double-subscribing. This is a robust practice.
        inputActions.Player.Attack.started -= OnAttackPressed;
        inputActions.Player.Attack.canceled -= OnAttackReleased;
       

        // Now, subscribe to the events.
        inputActions.Player.Attack.started += OnAttackPressed;
        inputActions.Player.Attack.canceled += OnAttackReleased;
        
    }

    // We will also create a function for unsubscribing.
    private void UnsubscribeFromEvents()
    {
        if (inputActions == null) return; // Nothing to unsubscribe from.

        Debug.Log("<color=orange>InputManager: Unsubscribing from events...</color>");
        inputActions.Player.Attack.started -= OnAttackPressed;
        inputActions.Player.Attack.canceled -= OnAttackReleased;
      
    }

    private void OnEnable()
    {
        // OnEnable's only job is to listen for scene changes.
        SceneManager.sceneLoaded += OnSceneLoaded;
        // We will also call Subscribe here for the very first time the game loads.
        if (inputActions != null)
        {
            SubscribeToEvents();
        }
    }

    private void OnDisable()
    {
        // OnDisable's only job is to stop listening for scene changes.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // And to clean up its event listeners.
        UnsubscribeFromEvents();
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
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // --- THIS IS THE GUARANTEED FIX ---
        // Every time a new scene loads, we do this sequence:

        // 1. Get the NEW, FRESH inputActions object from ZreyMovements.
        inputActions = ZreyMovements.inputActions;

        // 2. Call our new function to re-subscribe to all the events on this NEW object.
        SubscribeToEvents();

        // 3. The rest of your function is perfect.
        ScanSceneForReferences();
    }

    private void ScanSceneForReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerAttacks = playerObject.GetComponent<ZreyAttacks>();
        }
        if (playerAttacks == null)
        {
            Debug.LogError("InputManager could not find ZreyAttacks in the new scene!", this);
        }

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
 
}
