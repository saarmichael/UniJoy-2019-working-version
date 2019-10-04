using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.DirectInput;
using SlimDX.XInput;

namespace MoveController
{
    public class Joy
    {
        #region PROPERTIES
        DirectInput Input = new DirectInput();
        List<SlimDX.DirectInput.Joystick> sticks;
        int xValue;
        int yValue;
        int zValue;
        bool[] Pressed = Enumerable.Repeat(false, 200).ToArray();
        int[] CounterPressed = Enumerable.Repeat(0, 200).ToArray();
        bool A = false;
        int CounterA = 0;
        #endregion

        #region CONSTRUCTOR
        public Joy()
        {
            xValue = 0;
            yValue = 0;
            zValue = 0;
            GetJoysticks();
        }
        #endregion

        #region FUNCTIONS
        public void GetJoysticks()
        {
            sticks = new List<SlimDX.DirectInput.Joystick>(); // Creates the list of joysticks connected to the computer via USB.
            foreach (DeviceInstance device in Input.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                // Creates a joystick for each game device in USB Ports
                try
                {
                    SlimDX.DirectInput.Joystick stick = new SlimDX.DirectInput.Joystick(Input, device.InstanceGuid);
                    stick.Acquire();

                    // Gets the joysticks properties and sets the range for them.
                    foreach (DeviceObjectInstance deviceObject in stick.GetObjects())
                    {
                        if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                            stick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
                    }

                    // Adds how ever many joysticks are connected to the computer into the sticks list.
                    sticks.Add(stick);
                }
                catch (DirectInputException)
                {
                }
            }


        }

        public int getStates(Joystick stick)
        {
            JoystickState state = new JoystickState();
            state = stick.GetCurrentState();
            if (state.X != xValue || state.Y != yValue || state.Z != zValue)
            {
                xValue = state.X;
                yValue = state.Y;
                zValue = state.Z;
                //Console.WriteLine("X:" + xValue + " Y:" + yValue + " Z:" + zValue);

            }
            bool[] Buttons = state.GetButtons();
            for (int i = 0; i < Buttons.Count(); i++)
            {
                if (Buttons[i])
                {
                    if (Pressed[i] && CounterPressed[i] == 0)
                    {
                        Pressed[i] = false;
                        CounterPressed[i] = 1;
                        return 0;
                    }
                    else if (CounterPressed[i] == 0)
                    {
                        Pressed[i] = true;
                        //left is pressed
                        if (i == 10)
                        {
                            return 1;
                        }
                        //right is pressed
                        if (i == 11)
                        {
                            return 2;
                        }
                        //A is pressed
                        if (i == 0)
                        {
                            return 10;
                        }
                    }
                    else
                    {
                        CounterPressed[i] = 0;
                        return 0;
                    }
                }
                /*
                if (Buttons[0])
                {
                    if (A && CounterA == 0)
                    {
                        A = false;
                        CounterA = 1;
                        return false;
                    }
                    else if (CounterA == 0)
                    {
                        A = true;
                        return true;
                    }
                }
                else
                {
                    CounterA = 0;
                    return false;
                }

                return false;
                */

            }
            return 0;
        }

        public bool IsConnceted()
        {
            GetJoysticks();
            if (sticks.Count() == 0)
            {
                return false;
            }
            return true;
        }
        
        //check if the key pressed unlimited time
        public bool CheckIfPressed(int decision)
        {
            //this.GetJoysticks();
            //while (true)
            {
                if (this.sticks.Count == 0)
                {
                    Console.WriteLine("Please Connect Joystick");
                    //break;
                }
                if (this.getStates(this.sticks[0]) == decision)
                {
                    return true;
                }
            }
            return false;
        }

        public int KeyPressed()
        {
            //this.GetJoysticks();
            if (this.sticks.Count == 0)
            {   
                Console.WriteLine("Please Connect Joystick");
                Console.Beep(1000, 10000);
                return -1;
            }
            return this.getStates(this.sticks[0]);
        }
        #endregion
    }
}