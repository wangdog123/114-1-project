using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PressAnyButtonLoader : MonoBehaviour
{
    public string nextSceneName = "Loading";

    void Update()
    {
        // // 空白鍵 or 任意 joystick 鍵
        // if (IsJoystickPressed() || Input.anyKeyDown)
        // {
        //     TransitionController.Instance.ChangeScene(nextSceneName);
        // }
    }

    bool IsJoystickPressed()
    {
        // Joycon/手把按鍵會被視為 JoystickButton0~19
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown("joystick button " + i))
                return true;
        }
        return false;
    }


}
