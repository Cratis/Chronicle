// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace GrpcClients;

/// <summary>
/// Represents a method body reader for reading IL bytes.
/// </summary>
/// <remarks>
/// Based on: https://github.com/Rhotav/Dis2Msil, which in turn is based on:
/// https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body.
/// StringToByteArray from: https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array/321404.
/// </remarks>
public class MethodBodyReader
{
    readonly byte[] _il = [];
    readonly MethodInfo _mi = null!;
    readonly IEnumerable<FieldInfo> _fields;

    public MethodBodyReader(Module module, IEnumerable<FieldInfo> fields, string ilArrayAsString)
    {
        _fields = fields;
        Globals.LoadOpCodes();
        if (module != null)
        {
            _il = StringToByteArray(ilArrayAsString.Replace("-", string.Empty));
            ConstructInstructions(module);
        }
    }

    public MethodBodyReader(Module module, IEnumerable<FieldInfo> fields, byte[] ilArray)
    {
        _fields = fields;
        if (module != null)
        {
            _il = ilArray;

            ConstructInstructions(module);
        }
    }

    public IList<ILInstruction> Instructions { get; } = [];

    public static byte[] StringToByteArray(string hex) =>
        Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();

    public object GetReferencedOperand(Module module, int metadataToken)
    {
        var assemblyNames = module.Assembly.GetReferencedAssemblies();
        for (var i = 0; i < assemblyNames.Length; i++)
        {
            var modules = Assembly.Load(assemblyNames[i]).GetModules();
            if (modules.Length > 0)
            {
                var assemblyModule = Assembly.Load(assemblyNames[i]).GetModules().FirstOrDefault();
                if (assemblyModule == null) return typeof(MissingMethodException);
                return assemblyModule.ResolveType(metadataToken);
            }
        }

        return null!;
    }

    public string GetBodyCode()
    {
        var result = string.Empty;
        if (Instructions != null)
        {
            for (var i = 0; i < Instructions.Count; i++)
            {
                result += Instructions[i].GetCode() + "\n";
            }
        }
        return result;
    }

    public void Write(ILGenerator ilGenerator)
    {
        foreach (var instruction in Instructions)
        {
            switch (instruction.Operand)
            {
                case string operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case byte operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case sbyte operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case short operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case ushort operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case long operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case ulong operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case double operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                case float operand:
                    ilGenerator.Emit(instruction.Code, operand);
                    break;

                default:
                    switch (instruction.Code.OperandType)
                    {
                        case OperandType.InlineField:
                            var field = (FieldInfo)instruction.Operand;
                            var typeField = _fields.FirstOrDefault(_ => _.Name == field.Name);
                            if (typeField is not null)
                            {
                                field = typeField;
                            }
                            ilGenerator.Emit(instruction.Code, field);
                            break;

                        case OperandType.InlineMethod:
                            if (instruction.Operand is MethodInfo methodInfo)
                            {
                                ilGenerator.Emit(instruction.Code, methodInfo);
                            }
                            if (instruction.Operand is ConstructorInfo constructorInfo)
                            {
                                ilGenerator.Emit(instruction.Code, constructorInfo);
                            }
                            break;

                        case OperandType.InlineType:
                            var type = (Type)instruction.Operand;
                            ilGenerator.Emit(instruction.Code, type);
                            break;

                        default:
                            ilGenerator.Emit(instruction.Code);
                            break;
                    }
                    break;
            }
        }
    }

    void ConstructInstructions(Module module)
    {
        Globals.LoadOpCodes();
        var il = _il;
        var position = 0;
        while (position < il.Length)
        {
            var instruction = new ILInstruction();
            ushort value = il[position++];

            // get the operation code of the current instruction
            OpCode code;
            if (value != 0xfe)
            {
                code = Globals.SingleByteOpCodes[value];
            }
            else
            {
                value = il[position++];
                code = Globals.MultiByteOpCodes[value];
            }
            instruction.Code = code;
            instruction.Offset = position - 1;
            int metadataToken;

            // get the operand of the current operation
            switch (code.OperandType)
            {
                case OperandType.InlineBrTarget:
                    metadataToken = ReadInt32(ref position);
                    metadataToken += position;
                    instruction.Operand = metadataToken;
                    break;

                case OperandType.InlineField:
                    metadataToken = ReadInt32(ref position);
                    instruction.Operand = module.ResolveField(metadataToken)!;
                    break;

                case OperandType.InlineMethod:
                    metadataToken = ReadInt32(ref position);
                    try
                    {
                        instruction.Operand = module.ResolveMethod(metadataToken)!;
                    }
                    catch
                    {
                        instruction.Operand = module.ResolveMember(metadataToken)!;
                    }
                    break;

                case OperandType.InlineSig:
                    metadataToken = ReadInt32(ref position);
                    instruction.Operand = module.ResolveSignature(metadataToken);
                    break;

                case OperandType.InlineTok:
                    metadataToken = ReadInt32(ref position);
                    try
                    {
                        instruction.Operand = module.ResolveType(metadataToken);
                    }
                    catch
                    {
                        instruction.Operand = metadataToken;
                    }
                    break;

                case OperandType.InlineType:
                    metadataToken = ReadInt32(ref position);

                    // now we call the ResolveType always using the generic attributes type in order
                    // to support decompilation of generic methods and classes
                    // thanks to the guys from code project who commented on this missing feature
                    instruction.Operand = module.ResolveType(metadataToken, _mi.DeclaringType!.GetGenericArguments(), _mi.GetGenericArguments());
                    break;

                case OperandType.InlineI:
                    instruction.Operand = ReadInt32(ref position);
                    break;

                case OperandType.InlineI8:
                    instruction.Operand = ReadInt64(ref position);
                    break;

                case OperandType.InlineNone:
                    instruction.Operand = null!;
                    break;

                case OperandType.InlineR:
                    instruction.Operand = ReadDouble(ref position);
                    break;

                case OperandType.InlineString:
                    metadataToken = ReadInt32(ref position);
                    instruction.Operand = module.ResolveString(metadataToken);
                    break;

                case OperandType.InlineSwitch:
                    var count = ReadInt32(ref position);
                    var casesAddresses = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        casesAddresses[i] = ReadInt32(ref position);
                    }
                    var cases = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        cases[i] = position + casesAddresses[i];
                    }
                    break;

                case OperandType.InlineVar:
                    instruction.Operand = ReadUInt16(ref position);
                    break;

                case OperandType.ShortInlineBrTarget:
                    instruction.Operand = ReadSByte(ref position) + position;
                    break;

                case OperandType.ShortInlineI:
                    instruction.Operand = ReadSByte(ref position);
                    break;

                case OperandType.ShortInlineR:
                    instruction.Operand = ReadSingle(ref position);
                    break;

                case OperandType.ShortInlineVar:
                    instruction.Operand = ReadByte(ref position);
                    break;

                default:
                    throw new ArgumentException("Unknown operand type.");
            }

            Instructions.Add(instruction);
        }
    }

    ushort ReadUInt16(ref int position)
    {
        return (ushort)(_il[position++] | (_il[position++] << 8));
    }

    int ReadInt32(ref int position)
    {
        return _il[position++] | (_il[position++] << 8) | (_il[position++] << 0x10) | (_il[position++] << 0x18);
    }

    ulong ReadInt64(ref int position)
    {
        return (ulong)(_il[position++] | (_il[position++] << 8) | (_il[position++] << 0x10) | (_il[position++] << 0x18) | (_il[position++] << 0x20) | (_il[position++] << 0x28) | (_il[position++] << 0x30) | (_il[position++] << 0x38));
    }

    double ReadDouble(ref int position)
    {
        return _il[position++] | (_il[position++] << 8) | (_il[position++] << 0x10) | (_il[position++] << 0x18) | (_il[position++] << 0x20) | (_il[position++] << 0x28) | (_il[position++] << 0x30) | (_il[position++] << 0x38);
    }

    sbyte ReadSByte(ref int position)
    {
        return (sbyte)_il[position++];
    }
    byte ReadByte(ref int position)
    {
        return _il[position++];
    }

    float ReadSingle(ref int position)
    {
        return _il[position++] | (_il[position++] << 8) | (_il[position++] << 0x10) | (_il[position++] << 0x18);
    }
}
