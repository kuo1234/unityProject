using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string gameSceneName = "GameScene";
    public string menuSceneName = "startMenuScene";
    public string completeSceneName = "CompleteScene";

    public void LoadGameScene()
    {
        RoundResultStore.Clear();
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadMenuScene()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void RestartActiveScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (WasStartOrAgainPressed())
        {
            LoadGameScene();
        }
    }

    private bool WasStartOrAgainPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            return true;
        }

        if (UnityEngine.InputSystem.Gamepad.current != null &&
            UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            return true;
        }
#endif

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        return OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.All) ||
               OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.All);
#else
        return false;
#endif
    }
}
