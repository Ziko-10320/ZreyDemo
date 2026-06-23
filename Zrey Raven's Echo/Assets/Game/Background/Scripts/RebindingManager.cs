using UnityEngine;
using UnityEngine.InputSystem; // We absolutely need this.
using TMPro; // We need this to change the text on our buttons.
using System.Collections;
using System.Collections.Generic;
[System.Serializable]// This makes it show up in the Inspector
public class KeybindUI
{
    public string actionName;
    public TextMeshProUGUI buttonText;
    public InputActionReference actionReference; // We will use this to be safe
}
public class RebindingManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The small panel that says 'Press any key...'")]
    public GameObject listeningPanel;
    [Header("Keybind UI List")]
    [Tooltip("Drag all your keybinding buttons and their corresponding actions here.")]
    public List<KeybindUI> keybindUIList;
    // --- Private state variables ---
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private InputAction actionToRebind;
    private TextMeshProUGUI buttonTextToUpdate;
    [Tooltip("The panel that shows the 'Key Already In Use' error.")]
    public GameObject duplicateKeyErrorPanel;
  
    void Start()
    {
        // Make sure the listening panel is hidden at the start.
        if (listeningPanel != null)
        {
            listeningPanel.SetActive(false);
        }
        if (PlayerPrefs.HasKey("CustomKeybinds"))
        {
            string rebinds = PlayerPrefs.GetString("CustomKeybinds");
            ZreyMovements.inputActions.asset.LoadBindingOverridesFromJson(rebinds);

            // Refresh the UI texts so the buttons show the loaded keys
            foreach (KeybindUI ui in keybindUIList)
            {
                if (ui.actionReference != null && ui.buttonText != null)
                {
                    string currentBinding = ui.actionReference.action.bindings[0].effectivePath;
                    ui.buttonText.text = InputControlPath.ToHumanReadableString(
                        currentBinding,
                        InputControlPath.HumanReadableStringOptions.OmitDevice);
                }
            }
        }
    }

    // This is the main public function that our buttons will call.
    // It needs to know WHICH action to remap and WHICH button's text to update.
    public void StartRebinding(InputAction action, int bindingIndex, TextMeshProUGUI buttonText)
    {
        actionToRebind = action;
        buttonTextToUpdate = buttonText;

        if (listeningPanel != null) listeningPanel.SetActive(true);
        if (duplicateKeyErrorPanel != null) duplicateKeyErrorPanel.SetActive(false);

        actionToRebind.Disable();

        // This is now much simpler. We are NOT trying to filter duplicates here.
        rebindingOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation =>
            {
                // --- WE WILL CHECK FOR DUPLICATES HERE, MANUALLY ---
                CheckForDuplicateBindings();
            })
            .OnCancel(operation =>
            {
                // OnCancel now ONLY handles the user pressing Escape.
                CancelRebinding();
            })
            .Start();
    }
    private void CheckForDuplicateBindings()
    {
        // 1. Get the binding that the user just chose.
        var newBinding = actionToRebind.bindings[0]; // Assuming we are always changing the first binding for simplicity.

        // 2. Loop through ALL actions in our "Player" map.
        foreach (var action in ZreyMovements.inputActions.Player.Get())
        {
            // 3. Don't compare the action to itself.
            if (action == actionToRebind) continue;

            // 4. Loop through all the bindings on THIS action.
            foreach (var binding in action.bindings)
            {
                // 5. If the path of the new binding is the same as an existing one...
                if (newBinding.effectivePath == binding.effectivePath)
                {
                    // --- DUPLICATE FOUND! ---
                    Debug.LogError($"DUPLICATE FOUND! '{newBinding.effectivePath}' is already used by action '{action.name}'");

                    // A. Revert the change we just made.
                    actionToRebind.RemoveBindingOverride(0);

                    // B. Show the error message.
                    ShowDuplicateKeyError();

                    // C. Stop checking.
                    return;
                }
            }
        }

        // --- NO DUPLICATE FOUND ---
        // If we get through the whole loop without finding a duplicate,
        // then the binding is valid. We can now finish the process.
        Debug.Log("No duplicate found. Finishing rebind.");
        FinishRebinding();
    }
    public void ResetAllBindingsToDefault()
    {
        Debug.Log("<color=orange>--- RESETTING ALL BINDINGS TO DEFAULT ---</color>");

        // 1. RESET THE LOGIC
        // Loop through every action in the Player map and remove its overrides.
        // This has the .Get() fix so it will work.
        foreach (InputAction action in ZreyMovements.inputActions.Player.Get())
        {
            action.RemoveAllBindingOverrides();
        }

        // --- THIS IS THE FIX FOR THE BLOCK ACTION ---
        // We also need to tell the PlayerHealth script to reset its private override.
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            // We find the default binding for the Block action and send it.
            string defaultBlockBinding = ZreyMovements.inputActions.Player.Block.bindings[0].path;
            playerHealth.UpdateBlockBinding(defaultBlockBinding);
        }
        // --- END OF BLOCK FIX ---


        // 2. UPDATE THE UI TEXT
        // Now, loop through the list of UI elements we created.
        foreach (KeybindUI ui in keybindUIList)
        {
            // Get the current, correct binding path for the action.
            string currentBinding = ui.actionReference.action.bindings[0].effectivePath;

            // Update the button's text.
            ui.buttonText.text = InputControlPath.ToHumanReadableString(
                currentBinding,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
        PlayerPrefs.DeleteKey("CustomKeybinds");
        PlayerPrefs.Save();
        Debug.Log("All bindings and UI have been reset to default.");
    }
    private void ShowDuplicateKeyError()
    {
        // First, cancel the rebinding operation cleanly.
        CancelRebinding();

        // Then, if we have an error panel assigned...
        if (duplicateKeyErrorPanel != null)
        {
            // ...show it. That's it. No more timer.
            duplicateKeyErrorPanel.SetActive(true);
        }
    }
    public void CloseDuplicateKeyErrorPanel()
    {
        if (duplicateKeyErrorPanel != null)
        {
            duplicateKeyErrorPanel.SetActive(false);
        }
    }

    private void FinishRebinding()
    {
        // Clean up the rebinding operation.
        rebindingOperation.Dispose();

        // Re-enable the action with its new binding.
        actionToRebind.Enable();

        // Update the button's text to show the new key.
        // GetBindingDisplayString() is a powerful function that gives us a user-friendly name for the key.
        buttonTextToUpdate.text = InputControlPath.ToHumanReadableString(
            actionToRebind.bindings[0].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        // Hide the "listening" panel.
        if (listeningPanel != null)
        {
            listeningPanel.SetActive(false);
        }
        if (actionToRebind.name == "Block")
        {
            // 2. If YES, find the PlayerHealth script in the scene.
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                // 3. Get the new binding path that was just applied.
                string newBinding = actionToRebind.bindings[0].effectivePath;

                // 4. Call the public function on PlayerHealth and give it the new path.
                playerHealth.UpdateBlockBinding(newBinding);
            }
        }
        string rebinds = ZreyMovements.inputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("CustomKeybinds", rebinds);
        PlayerPrefs.Save();
    }

    // This function will be called by our "Cancel" button.
   public void CancelRebinding()
{
    // Make sure we have a rebinding operation to cancel
    if (rebindingOperation != null)
    {
        // This is the official way to cancel it.
        rebindingOperation.Dispose();
    }

    // Re-enable the action with its original binding.
    if (actionToRebind != null)
    {
        actionToRebind.Enable();
    }

    // Hide the "Press any key..." panel.
    if (listeningPanel != null)
    {
        listeningPanel.SetActive(false);
    }

    // Optional: Update the button text back to what it was, just in case.
    if (buttonTextToUpdate != null && actionToRebind != null)
    {
        int bindingIndex = 0; // Assuming we are always changing the first binding
        buttonTextToUpdate.text = InputControlPath.ToHumanReadableString(
            actionToRebind.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    Debug.Log("Rebinding Canceled.");
}
}
