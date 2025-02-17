﻿namespace D3D12Testing.Input.Events
{
    public class GamepadRemappedEventArgs : EventArgs
    {
        public GamepadRemappedEventArgs()
        {
            Mapping = string.Empty;
        }

        public GamepadRemappedEventArgs(string mapping)
        {
            Mapping = mapping;
        }

        public string Mapping { get; internal set; }
    }
}