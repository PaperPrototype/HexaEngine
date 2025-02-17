﻿namespace VkTesting.Input.Events
{
    using VkTesting.Input;

    public class JoystickHatMotionEventArgs : EventArgs
    {
        public JoystickHatMotionEventArgs()
        {
        }

        public JoystickHatMotionEventArgs(int hat, JoystickHatState state)
        {
            Hat = hat;
            State = state;
        }

        public int Hat { get; internal set; }

        public JoystickHatState State { get; internal set; }
    }
}