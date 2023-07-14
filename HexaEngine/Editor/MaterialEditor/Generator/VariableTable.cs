﻿namespace HexaEngine.Editor.MaterialEditor.Generator
{
    using HexaEngine.Editor.MaterialEditor.Generator.Structs;
    using Silk.NET.DirectWrite;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public class VariableTable
    {
        private readonly List<Identifier> identifiers = new();

        private readonly List<UnorderedAccessView> unorderedAccessViews = new();
        private readonly List<ShaderResourceView> shaderResourceViews = new();
        private readonly List<ConstantBuffer> constantBuffers = new();
        private readonly List<SamplerState> samplers = new();
        private readonly List<Operation> operations = new();
        private readonly List<Struct> structs = new();
        private readonly List<Include> includes = new();
        private readonly List<Macro> macros = new();
        private readonly List<Method> methods = new();

        private uint srvCounter;
        private uint uavCounter;
        private uint cbvCounter;
        private uint sptCounter;

        public void SetBaseSrv(uint baseSlot)
        {
            srvCounter = baseSlot;
        }

        public void SetBaseUav(uint baseSlot)
        {
            uavCounter = baseSlot;
        }

        public void SetBaseCbv(uint baseSlot)
        {
            cbvCounter = baseSlot;
        }
        public void SetBaseSampler(uint baseSlot)
        {
            sptCounter = baseSlot;
        }


        public int OperationCount => operations.Count;

        public void Build(CodeWriter builder)
        {
            builder.WriteLine("// ------------------------------------------------------------------------------");
            builder.WriteLine("// <auto-generated>");
            builder.WriteLine($"//     This code was generated by HexaEngine ShaderGenerator {ShaderGenerator.VersionString}");
            builder.WriteLine("//");
            builder.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
            builder.WriteLine("//     the code is regenerated.");
            builder.WriteLine("// </auto-generated>");
            builder.WriteLine("// ------------------------------------------------------------------------------");
            builder.WriteLine();

            builder.WriteLine("/// Includes");
            for (int i = 0; i < includes.Count; i++)
            {
                includes[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Macros");
            for (int i = 0; i < macros.Count; i++)
            {
                macros[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Structures");
            for (int i = 0; i < structs.Count; i++)
            {
                structs[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Unordered Access Views");
            for (int i = 0; i < unorderedAccessViews.Count; i++)
            {
                unorderedAccessViews[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Shader Resource Views");
            for (int i = 0; i < shaderResourceViews.Count; i++)
            {
                shaderResourceViews[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Samplers");
            for (int i = 0; i < samplers.Count; i++)
            {
                samplers[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Constant Buffers");
            for (int i = 0; i < constantBuffers.Count; i++)
            {
                constantBuffers[i].Build(builder);
            }
            builder.WriteLine();
            builder.WriteLine("/// Methods");
            for (int i = 0; i < methods.Count; i++)
            {
                methods[i].Build(builder);
            }
            builder.WriteLine();
        }

        public static string CastTo(SType type)
        {
            if (type.IsScalar || type.IsVector || type.IsMatrix || type.IsStruct)
            {
                return $"({type.Name})";
            }
            throw new InvalidCastException();
        }

        public static string FromCastTo(SType from, SType to)
        {
            if (from == to)
            {
                return string.Empty;
            }

            return CastTo(to);
        }

        public static bool NeedCastPerComponentMath(SType a, SType b)
        {
            return a != b && !a.IsScalar && !b.IsScalar;
        }

        public void Clear()
        {
            unorderedAccessViews.Clear();
            shaderResourceViews.Clear();
            constantBuffers.Clear();
            identifiers.Clear();
            operations.Clear();
            samplers.Clear();
            structs.Clear();
            macros.Clear();
            includes.Clear();
            srvCounter = 0;
            uavCounter = 0;
            cbvCounter = 0;
            sptCounter = 0;
        }

        public bool IsIncluded(string name)
        {
            for (int i = 0; i < includes.Count; i++)
            {
                var include = includes[i];
                if (include.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMethodDefined(string name)
        {
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                if (method.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOfInclude(string name)
        {
            for (int i = 0; i < includes.Count; i++)
            {
                var include = includes[i];
                if (include.Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOfMethod(string name)
        {
            for (int i = 0; i < methods.Count; i++)
            {
                var methods = this.methods[i];
                if (methods.Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void AddInclude(string name)
        {
            if (!IsIncluded(name))
            {
                includes.Add(new() { Name = name });
            }
        }

        public void RemoveInclude(string name)
        {
            var index = IndexOfInclude(name);
            if (index != -1)
            {
                includes.RemoveAt(index);
            }
        }

        public bool IsMacroDefined(string name)
        {
            for (int i = 0; i < macros.Count; i++)
            {
                if (macros[i].Name == name)
                    return true;
            }
            return false;
        }

        public int IndexOfMacro(string name)
        {
            for (int i = 0; i < macros.Count; i++)
            {
                var macros = this.macros[i];
                if (macros.Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void AddMacro(string name, string definition)
        {
            if (!IsMacroDefined(name))
            {
                macros.Add(new() { Name = name, Definition = definition });
            }
        }

        public void RemoveMacro(string name)
        {
            var index = IndexOfMacro(name);
            if (index != -1)
            {
                macros.RemoveAt(index);
            }
        }

        public void AddMethod(string name, string signature, string returnType, string body)
        {
            if (!IsMethodDefined(name))
            {
                methods.Add(new() { Name = name, Signature = signature, ReturnType = returnType, Body = body });
            }
        }

        public void RemoveMethod(string name)
        {
            var index = IndexOfMethod(name);
            if (index != -1)
            {
                methods.RemoveAt(index);
            }
        }

        public void AddKeyword(string name)
        {
            identifiers.Add(new(name));
        }

        public UnorderedAccessView AddUnorderedAccessView(UnorderedAccessView unorderedAccessView)
        {
            identifiers.Add(new(unorderedAccessView.Name));
            unorderedAccessView.Slot = uavCounter++;
            unorderedAccessViews.Add(unorderedAccessView);
            return unorderedAccessView;
        }

        public ShaderResourceView AddShaderResourceView(ShaderResourceView shaderResourceView)
        {
            identifiers.Add(new(shaderResourceView.Name));
            shaderResourceView.Slot = srvCounter++;
            shaderResourceViews.Add(shaderResourceView);
            return shaderResourceView;
        }

        public ConstantBuffer AddConstantBuffer(ConstantBuffer constantBuffer)
        {
            identifiers.Add(new(constantBuffer.Name));
            constantBuffer.Slot = cbvCounter++;
            constantBuffers.Add(constantBuffer);
            return constantBuffer;
        }

        public SamplerState AddSamplerState(SamplerState samplerState)
        {
            identifiers.Add(new(samplerState.Name));
            samplerState.Slot = sptCounter++;
            samplers.Add(samplerState);
            return samplerState;
        }

        public Operation AddVariable(Operation variable)
        {
            identifiers.Add(new(variable.Name));
            operations.Add(variable);
            return variable;
        }

        public Struct AddStruct(Struct @struct)
        {
            identifiers.Add(new(@struct.Name));
            structs.Add(@struct);
            return @struct;
        }

        public UnorderedAccessView GetUnorderedAccessView(string name)
        {
            for (int i = 0; i < unorderedAccessViews.Count; i++)
            {
                if (unorderedAccessViews[i].Name == name)
                {
                    return unorderedAccessViews[i];
                }
            }
            return default;
        }

        public ShaderResourceView GetShaderResourceView(string name)
        {
            for (int i = 0; i < shaderResourceViews.Count; i++)
            {
                if (shaderResourceViews[i].Name == name)
                {
                    return shaderResourceViews[i];
                }
            }
            return default;
        }

        public ConstantBuffer GetConstantBuffer(string name)
        {
            for (int i = 0; i < constantBuffers.Count; i++)
            {
                if (constantBuffers[i].Name == name)
                {
                    return constantBuffers[i];
                }
            }
            return default;
        }

        public SamplerState GetSamplerState(string name)
        {
            for (int i = 0; i < samplers.Count; i++)
            {
                if (samplers[i].Name == name)
                {
                    return samplers[i];
                }
            }
            return default;
        }

        public Operation GetVariable(string name)
        {
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Name == name)
                {
                    return operations[i];
                }
            }
            return default;
        }

        public void AddRef(string name, Operation refTo)
        {
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Name == name)
                {
                    var va = operations[i];
                    va.RefCount++;
                    refTo.References.Add(va);
                }
            }
        }

        public Operation GetVariable(int id)
        {
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Id == id)
                {
                    return operations[i];
                }
            }
            return default;
        }

        public Struct GetStruct(string name)
        {
            for (int i = 0; i < structs.Count; i++)
            {
                if (structs[i].Name == name)
                {
                    return structs[i];
                }
            }
            return default;
        }

        public bool VariableExists(string name)
        {
            for (int i = 0; i < identifiers.Count; i++)
            {
                if (string.Compare(identifiers[i].Name, name, true) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetUniqueName(string name)
        {
            string newName = name;
            int al = 0;
            while (VariableExists(newName))
            {
                newName = $"{name}{al.ToString(CultureInfo.InvariantCulture)}";
                al++;
            }

            return newName;
        }

        public Operation GetOperation(int i)
        {
            return operations[i];
        }
    }
}