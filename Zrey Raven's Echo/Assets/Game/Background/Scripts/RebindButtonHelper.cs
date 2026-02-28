using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RebindButtonHelper : MonoBehaviour
{
    [Header("Action To Rebind")]
    [Tooltip("The name of the action to rebind (e.g., 'Move', 'Jump', 'Attack').")]
    public string actionName;

    [Tooltip("The specific binding index to change. 0 is usually the primary binding.")]
    public int bindingIndex = 0;

    private TextMeshProUGUI buttonText;
    private RebindingManager rebindingManager;

    void Awake()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        rebindingManager = FindObjectOfType<RebindingManager>();
    }

    void Start()
    {
        UpdateText();
    }

    public void OnButtonClick()
    {
        if (rebindingManager != null)
        {
            // --- THIS IS THE FIX ---
            // We get the action directly from the static variable in ZreyMovements.
            InputAction action = ZreyMovements.inputActions.asset.FindAction(actionName);
            // --- END OF FIX ---

            if (action != null)
            {
                // Tell the manager to start, passing the actual action object.
                rebindingManager.StartRebinding(action, bindingIndex, buttonText);
            }
            else
            {
                Debug.LogError($"Action '{actionName}' not found!", this);
            }
        }
    }

    public void UpdateText()
    {
        if (buttonText != null)
        {
            // --- THIS IS THE FIX ---
            // We get the action directly from the static variable in ZreyMovements.
            InputAction action = ZreyMovements.inputActions.asset.FindAction(actionName);
            // --- END OF FIX ---

            if (action != null)
            {
                buttonText.text = InputControlPath.ToHumanReadableString(
                    action.bindings[bindingIndex].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
        }
    }
}
