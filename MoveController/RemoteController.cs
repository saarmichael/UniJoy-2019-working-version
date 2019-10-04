using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using log4net;
using log4net;
using SharpDX.XInput;

namespace MoveController
{
    public class RemoteController
    {
        private ILog log;
        private Controller controller;
        private bool isConnected = false;
        private Joy _joystick;

        public RemoteController(ILog logger = null)
        {
            log = logger;
            Initialize();
            _joystick = new Joy();
        }

        public RemoteController()
        {
            Initialize();
            _joystick = new Joy();
        }

        private void Initialize()
        {
            // Initialize XInput
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            // Get 1st controller available
            controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }
            if (controller == null)
            {
                //log.Error("No XInput controller installed. Connect the Remote controller.");
            }
            else
            {
                //log.Info("Found a XInput controller available");
                isConnected = true;
            }

        }

        private GamepadButtonFlags listen()
        {
            // Poll events from joystick
            if (isConnected)
            {
                var previousState = controller.GetState();
                while (controller.IsConnected)
                {
                    var state = controller.GetState();
                    GamepadButtonFlags lastPressedKey = GamepadButtonFlags.None;
                    GamepadButtonFlags currentPressedKey = GamepadButtonFlags.None;
                    // if A button was pressed
                    if (previousState.PacketNumber != state.PacketNumber && state.Gamepad.Buttons != GamepadButtonFlags.None)
                    {
                        if (state.Gamepad.Buttons == GamepadButtonFlags.A)
                            return GamepadButtonFlags.A;
                        lastPressedKey = currentPressedKey;
                        currentPressedKey = state.Gamepad.Buttons;
                    }
                    if (lastPressedKey != currentPressedKey)
                        Console.WriteLine(currentPressedKey);
                    Thread.Sleep(10);
                    previousState = state;
                }
            }
            return GamepadButtonFlags.None;
        }

        public bool IsStartButtonPressed()
        {
            //GamepadButtonFlags pressedKey = listen();
            //return pressedKey==GamepadButtonFlags.A;
            if(_joystick.CheckIfPressed(10))
            {
                _joystick = new Joy();
                return true;
            }
            return false;

        }
        public byte SubjectMovementChoice()
        {
            //According to RatDecision enum: Left = 1 Right = 2, Up = 3, Down = 4
            int value = _joystick.KeyPressed();
            if (value != 0)
            {
                _joystick = new Joy();
                return (byte)value;
            }
            return 0;

        }
    }
}
