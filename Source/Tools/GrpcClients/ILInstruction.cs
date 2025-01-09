// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Emit;

namespace GrpcClients;

/// <summary>
/// Represents a method body reader for reading IL bytes.
/// </summary>
/// <remarks>
/// Based on: https://github.com/Rhotav/Dis2Msil, which in turn is based on:
/// https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body.
/// </remarks>
public class ILInstruction
{
#pragma warning disable CA1721 // Property names should not match get methods
    public OpCode Code { get; set; } = OpCodes.Nop;
#pragma warning restore CA1721 // Property names should not match get methods

    public object Operand { get; set; } = null!;

#pragma warning disable CA1819 // Properties should not return arrays
    public byte[] OperandData { get; set; } = [];
#pragma warning restore CA1819 // Properties should not return arrays

    public int Offset { get; set; }

    /// <summary>
    /// Returns a friendly string. representation of this instruction.
    /// </summary>
    /// <returns>String representation.</returns>
    public string GetCode()
    {
        var result = string.Empty;
        result += GetExpandedOffset(Offset) + " : " + Code;
        if (Operand != null)
        {
            switch (Code.OperandType)
            {
                case OperandType.InlineField:
                    var fOperand = (System.Reflection.FieldInfo)Operand;
                    result += " " + Globals.ProcessSpecialTypes(fOperand.FieldType.ToString()) + " " +
                        Globals.ProcessSpecialTypes(fOperand.ReflectedType!.ToString()) +
                        "::" + fOperand.Name;
                    break;

                case OperandType.InlineMethod:
                    try
                    {
                        var operand = (System.Reflection.MethodInfo)Operand;
                        result += " ";
                        if (!operand.IsStatic) result += "instance ";
                        result += Globals.ProcessSpecialTypes(operand.ReturnType.ToString()) +
                            " " + Globals.ProcessSpecialTypes(operand.ReflectedType!.ToString()) +
                            "::" + operand.Name + "()";
                    }
                    catch
                    {
                        try
                        {
                            var operand = (System.Reflection.ConstructorInfo)Operand;
                            result += " ";
                            if (!operand.IsStatic) result += "instance ";
                            result += "void " +
                                Globals.ProcessSpecialTypes(operand.ReflectedType!.ToString()) +
                                "::" + operand.Name + "()";
                        }
                        catch
                        {
                        }
                    }
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    result += " " + GetExpandedOffset((int)Operand);
                    break;

                case OperandType.InlineType:
                    result += " " + Globals.ProcessSpecialTypes(Operand.ToString()!);
                    break;

                case OperandType.InlineString:
                    if (Operand.ToString() == "\r\n") result += " \"\\r\\n\"";
                    else result += $" \"{Operand}\"";
                    break;

                case OperandType.ShortInlineVar:
                case OperandType.InlineI:
                case OperandType.InlineI8:
                case OperandType.InlineR:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineR:
                    result += Operand.ToString();
                    break;

                case OperandType.InlineTok:
                    if (Operand is Type type)
                        result += type.FullName;
                    else
                        result += "not supported";
                    break;

                default: result += "not supported"; break;
            }
        }

        return result;
    }

    string GetExpandedOffset(long offset)
    {
        var result = offset.ToString();
        for (var i = 0; result.Length < 4; i++)
        {
            result = "0" + result;
        }
        return result;
    }
}
