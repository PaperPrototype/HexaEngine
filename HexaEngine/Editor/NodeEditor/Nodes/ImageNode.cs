﻿namespace HexaEngine.Editor.NodeEditor.Nodes
{
    using BepuPhysics;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Rendering;
    using ImGuiNET;
    using System;
    using System.Numerics;

    public class ImageNode : Node
    {
        private IShaderResourceView? image;
        private nint imgId;

        public ImageNode(NodeEditor graph, string name, bool removable, bool isStatic) : base(graph, name, removable, isStatic)
        {
            CreatePin("out Image", PinKind.Output, PinType.Texture2D, ImNodesNET.PinShape.Quad);
        }

        public ImageNode(NodeEditor graph, string name, PinType type, bool removable, bool isStatic) : base(graph, name, removable, isStatic)
        {
            CreatePin("out Image", PinKind.Output, type, ImNodesNET.PinShape.Quad);
        }

        public IShaderResourceView? Image
        {
            get => image;
            set
            {
                if (image != null)
                {
                    image.OnDisposed -= OnDisposed;
                    ImGuiRenderer.UnregisterTexture(image);
                }

                if (value == null)
                {
                    image = value;
                }
                else
                {
                    image = value;
                    value.OnDisposed += OnDisposed;
                    imgId = ImGuiRenderer.RegisterTexture(value);
                }
            }
        }

        public Vector2 Size = new(128, 128);

        private void OnDisposed(object? sender, EventArgs e)
        {
            if (sender is IShaderResourceView srv)
            {
                ImGuiRenderer.UnregisterTexture(srv);
                srv.OnDisposed -= OnDisposed;
            }
        }

        protected override void DrawContent()
        {
            ImGui.Image(imgId, Size);
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }

    public class ImageCubeNode : Node
    {
        private IShaderResourceView? image;
        private nint imgId;

        public ImageCubeNode(NodeEditor graph, string name, bool removable, bool isStatic) : base(graph, name, removable, isStatic)
        {
            CreatePin("out Image", PinKind.Output, PinType.TextureCube, ImNodesNET.PinShape.Quad);
        }

        public IShaderResourceView? Image
        {
            get => image;
            set
            {
                if (image != null)
                {
                    image.OnDisposed -= OnDisposed;
                    ImGuiRenderer.UnregisterTexture(image);
                }

                if (value == null)
                {
                    image = value;
                }
                else
                {
                    image = value;
                    value.OnDisposed += OnDisposed;
                    imgId = ImGuiRenderer.RegisterTexture(value);
                }
            }
        }

        public Vector2 Size = new(128, 128);

        private void OnDisposed(object? sender, EventArgs e)
        {
            if (sender is IShaderResourceView srv)
            {
                ImGuiRenderer.UnregisterTexture(srv);
                srv.OnDisposed -= OnDisposed;
            }
        }

        protected override void DrawContent()
        {
            //ImGui.Image(imgId, Size);
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}