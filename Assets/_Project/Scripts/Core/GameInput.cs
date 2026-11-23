using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HeavenlyWarrior
{
    /// <summary>
    /// Leitura de input centralizada. Funciona com o Input System novo (por defeito no Unity 6)
    /// e com o Input Manager antigo, para não depender de nenhuma configuração no Editor.
    ///
    /// Teclado: A/D ou setas = mover, Espaço/W/Seta cima = saltar, J ou clique esquerdo = atacar,
    /// Shift esquerdo ou L = dash, R = recomeçar.
    /// Comando: analógico/cruz = mover, A (sul) = saltar, X (oeste) = atacar, B (este) ou RB = dash.
    /// </summary>
    public static class GameInput
    {
        const float StickDeadZone = 0.3f;

        public static float MoveX
        {
            get
            {
                float x = 0f;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                }
                var g = Gamepad.current;
                if (g != null && Mathf.Approximately(x, 0f))
                {
                    float stick = g.leftStick.x.ReadValue();
                    float dpad = g.dpad.x.ReadValue();
                    if (Mathf.Abs(stick) > StickDeadZone) x = stick;
                    else if (Mathf.Abs(dpad) > StickDeadZone) x = dpad;
                }
#else
                x = Input.GetAxisRaw("Horizontal");
#endif
                return Mathf.Clamp(x, -1f, 1f);
            }
        }

        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                var g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame))
                    || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                    || Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
            }
        }

        public static bool JumpHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                var g = Gamepad.current;
                return (k != null && (k.spaceKey.isPressed || k.wKey.isPressed || k.upArrowKey.isPressed))
                    || (g != null && g.buttonSouth.isPressed);
#else
                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)
                    || Input.GetKey(KeyCode.JoystickButton0);
#endif
            }
        }

        public static bool AttackPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                var g = Gamepad.current;
                var m = Mouse.current;
                return (k != null && k.jKey.wasPressedThisFrame)
                    || (m != null && m.leftButton.wasPressedThisFrame)
                    || (g != null && g.buttonWest.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton2);
#endif
            }
        }

        public static bool DashPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                var g = Gamepad.current;
                return (k != null && (k.leftShiftKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame))
                    || (g != null && (g.buttonEast.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame));
#else
                return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.L)
                    || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton5);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                var g = Gamepad.current;
                return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton7);
#endif
            }
        }
    }
}
