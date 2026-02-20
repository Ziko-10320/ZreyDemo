using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.SceneManagement;
public class InputManager : MonoBehaviour
{
    // --- The Singleton Pattern ---
    public static InputManager Instance { get; private set; }
    // ---
    public static event Action OnInteractPressed;
    // --- Input Actions ---
    private InputSystem_Actions inputActions;
    
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

        inputActions.Player.Counter.performed += OnInteractInput;
        inputActions.Player.Enable();
        Debug.Log("<color=cyan>InputManager: OnEnable() - NOW LISTENING FOR INPUTS.</color>");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // --- THIS IS THE FIX ---
        inputActions.Player.Attack.started -= OnAttackPressed;
       
        inputActions.Player.Attack.canceled -= OnAttackReleased;
        // --- END OF FIX ---

        inputActions.Player.Counter.performed -= OnInteractInput;
        inputActions.Player.Disable();
        Debug.Log("<color=orange>InputManager: OnDisable() - STOPPED LISTENING.</color>");
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
    private void OnInteractInput(InputAction.CallbackContext context)
    {
        // This method's ONLY job is to fire the event.
        // It has no knowledge of finishers or counters.
        OnInteractPressed?.Invoke();
        Debug.LogWarning("--- InputManager: OnInteractPressed EVENT FIRED ---");
    }

}
