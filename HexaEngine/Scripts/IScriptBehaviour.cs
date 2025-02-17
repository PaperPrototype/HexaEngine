﻿namespace HexaEngine.Scripts
{
    using HexaEngine.Scenes;

    public interface IScriptBehaviour
    {
        public GameObject GameObject { get; set; }

        public void Awake()
        {
        }

        public void FixedUpdate()
        {
        }

        public void Update()
        {
        }

        public void Destroy()
        {
        }
    }
}