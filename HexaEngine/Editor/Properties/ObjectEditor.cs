﻿namespace HexaEngine.Editor.Properties
{
    using Hexa.NET.ImGui;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.UI;
    using HexaEngine.Editor.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    /// <summary>
    /// Implementation of the <see cref="IObjectEditor"/> interface for managing object editing in a graphical context.
    /// </summary>
    public class ObjectEditor : IObjectEditor
    {
        private readonly List<IObjectEditorElement> elements = new();
        private readonly List<EditorCategory> categories = new();
        private readonly Dictionary<string, EditorCategory> nameToCategory = new();

        private ImGuiName guiName;
        private readonly bool isHidden;
        private readonly Type type;
        private object? instance;

        private static PropertyInfo[] GetBasePropertiesFirst([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            var orderList = new List<Type>();
            var iteratingType = type;
            do
            {
                orderList.Insert(0, iteratingType);
                iteratingType = iteratingType.BaseType;
            } while (iteratingType != null);

            var props = type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(x => orderList.IndexOf(x.DeclaringType))
                .ToArray();

            return props;
        }

        private static MethodInfo[] GetBaseMethodsFirst([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
            var orderList = new List<Type>();
            var iteratingType = type;
            do
            {
                orderList.Insert(0, iteratingType);
                iteratingType = iteratingType.BaseType;
            } while (iteratingType != null);

            var props = type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(x => orderList.IndexOf(x.DeclaringType))
                .ToArray();

            return props;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEditor"/> class.
        /// </summary>
        /// <param name="type">The type of the object being edited.</param>
        /// <param name="factories">A list of property editor factories used to create property editors.</param>
        public ObjectEditor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type type, List<IPropertyEditorFactory> factories)
        {
            this.type = type;
            PropertyInfo[] properties = GetBasePropertiesFirst(type); //type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
            MethodInfo[] methods = GetBaseMethodsFirst(type); //type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);

            var componentNameAttr = type.GetCustomAttribute<EditorComponentAttribute>();
            if (componentNameAttr == null)
            {
                guiName = new(type.Name);
            }
            else
            {
                guiName = new(componentNameAttr.Name);
                isHidden = componentNameAttr.IsHidden;
            }

            var nodeNameAttr = type.GetCustomAttribute<EditorGameObjectAttribute>();
            if (nodeNameAttr != null)
            {
                guiName = new(nodeNameAttr.Name);
            }

            foreach (var property in properties)
            {
                var nameAttr = property.GetCustomAttribute<EditorPropertyAttribute>();
                if (nameAttr == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(nameAttr.Name))
                {
                    nameAttr.Name = property.Name;
                }

                var categoryAttr = property.GetCustomAttribute<EditorCategoryAttribute>();
                var conditionAttr = property.GetCustomAttribute<EditorPropertyConditionAttribute>();

                for (int i = 0; i < factories.Count; i++)
                {
                    if (factories[i].TryCreate(property, nameAttr, out var editor))
                    {
                        PropertyEditorObjectEditorElement element = new(editor)
                        {
                            Condition = conditionAttr?.Condition,
                            ConditionMode = conditionAttr?.Mode ?? EditorPropertyConditionMode.None
                        };

                        if (categoryAttr != null)
                        {
                            var category = CreateOrGetCategory(categoryAttr);
                            category.Elements.Add(element);
                        }
                        else
                        {
                            elements.Add(element);
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo? method = methods[i];
                var buttonAttr = method.GetCustomAttribute<EditorButtonAttribute>();
                if (buttonAttr == null)
                {
                    continue;
                }

                var categoryAttr = method.GetCustomAttribute<EditorCategoryAttribute>();
                var conditionAttr = method.GetCustomAttribute<EditorPropertyConditionAttribute>();

                EditorButtonObjectEditorElement element = new(new(buttonAttr, method))
                {
                    Condition = conditionAttr?.Condition,
                    ConditionMode = conditionAttr?.Mode ?? EditorPropertyConditionMode.None
                };

                if (categoryAttr != null)
                {
                    var category = CreateOrGetCategory(categoryAttr);
                    category.Elements.Add(element);
                }
                else
                {
                    elements.Add(element);
                    break;
                }
            }

            Queue<EditorCategory> removeQueue = new();
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                if (category.CategoryParent != null)
                {
                    var parent = CreateOrGetCategory(category.CategoryParent);
                    parent.ChildCategories.Add(category);
                    removeQueue.Enqueue(category);
                }
            }

            while (removeQueue.TryDequeue(out var category))
            {
                categories.Remove(category);
            }

            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].Sort();
            }
        }

        private EditorCategory CreateOrGetCategory(EditorCategoryAttribute categoryAttr)
        {
            if (nameToCategory.TryGetValue(categoryAttr.Name, out var editor))
            {
                return editor;
            }

            editor = new(categoryAttr);
            nameToCategory.Add(categoryAttr.Name, editor);
            categories.Add(editor);
            return editor;
        }

        private EditorCategory CreateOrGetCategory(string category)
        {
            if (nameToCategory.TryGetValue(category, out var editor))
            {
                return editor;
            }

            editor = new(category);
            nameToCategory.Add(category, editor);
            categories.Add(editor);
            return editor;
        }

        /// <summary>
        /// Gets the symbol associated with the object editor.
        /// </summary>
        public string Symbol { get; } = "?";

        /// <summary>
        /// Gets the name associated with the object editor.
        /// </summary>
        public string Name => guiName.Name;

        /// <summary>
        /// Gets the type of the object being edited.
        /// </summary>
        public Type Type => type;

        /// <summary>
        /// Gets or sets the instance of the object being edited.
        /// </summary>
        public object? Instance { get => instance; set => instance = value; }

        /// <summary>
        /// Gets a value indicating whether the object editor is empty.
        /// </summary>
        public bool IsEmpty => elements.Count == 0 && categories.Count == 0;

        /// <summary>
        /// Gets a value indicating whether the object editor is hidden.
        /// </summary>
        public bool IsHidden => isHidden;

        /// <summary>
        /// Gets or sets a value indicating whether to skip the table setup when drawing the object editor.
        /// </summary>
        public bool NoTable { get; set; }

        /// <summary>
        /// Draws the object editor within the specified graphics context.
        /// </summary>
        /// <param name="context">The graphics context used for drawing.</param>
        public virtual bool Draw(IGraphicsContext context)
        {
            if (instance == null)
            {
                return false;
            }

            if (!NoTable)
            {
                ImGui.BeginTable(guiName.RawId, 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PreciseWidths);
                ImGui.TableSetupColumn("", 0.0f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            }

            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].UpdateVisibility(instance);
            }

            bool changed = false;

            for (int i = 0; i < elements.Count; i++)
            {
                changed |= elements[i].Draw(context, instance);
            }

            object? nullObj = null;

            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];

                changed |= category.Draw(context, instance, ref nullObj);
            }

            if (!NoTable)
            {
                ImGui.EndTable();
            }

            return changed;
        }

        /// <summary>
        /// Disposes of the resources used by the object editor.
        /// </summary>
        public void Dispose()
        {
        }
    }
}