﻿namespace HexaEngine.Editor.Properties.Editors
{
    using HexaEngine.Core.Editor.Attributes;
    using HexaEngine.Core.Editor.Properties;
    using HexaEngine.Core.Scripts;
    using ImGuiNET;
    using System;

    public class TypePropertyEditor : IPropertyEditor
    {
        private readonly string id;
        private readonly string name;
        private readonly Type targetType;

        public TypePropertyEditor(EditorPropertyAttribute attribute)
        {
            this.id = Guid.NewGuid().ToString();
            name = attribute.Name;
            targetType = attribute.TargetType ?? throw new InvalidOperationException();
        }

        public Type TargetType => targetType;

        public bool Draw(object instance, ref object? value)
        {
            var types = AssemblyManager.GetAssignableTypes(targetType);
            var names = AssemblyManager.GetAssignableTypeNames(targetType);

            int index;
            if (value == null)
            {
                index = -1;
            }
            else
            {
                index = types.IndexOf((Type)value);
            }
            if (ImGui.Combo($"{name}##{id}", ref index, names, names.Length))
            {
                value = types[index];
                return true;
            }
            return false;
        }
    }
}