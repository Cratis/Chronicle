// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace GrpcClients;

/// <summary>
/// Represents global cache.
/// </summary>
/// <remarks>
/// Based on: https://github.com/Rhotav/Dis2Msil, which in turn is based on:
/// https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body.
/// </remarks>
public static class Globals
{
    public static IDictionary<int, object> Cache { get; } = new Dictionary<int, object>();

#pragma warning disable CA1819 // Properties should not return arrays
    public static OpCode[] MultiByteOpCodes { get; private set; } = [];
    public static OpCode[] SingleByteOpCodes { get; private set; } = [];
    public static Module[] modules { get; } = [];
#pragma warning restore CA1819 // Properties should not return arrays

    public static void LoadOpCodes()
    {
        SingleByteOpCodes = new OpCode[0x100];
        MultiByteOpCodes = new OpCode[0x100];
        var infoArray1 = typeof(OpCodes).GetFields();
        for (var num1 = 0; num1 < infoArray1.Length; num1++)
        {
            var info1 = infoArray1[num1];
            if (info1.FieldType == typeof(OpCode))
            {
                var code1 = (OpCode)info1.GetValue(null)!;
                var num2 = (ushort)code1.Value;
                if (num2 < 0x100)
                {
                    SingleByteOpCodes[num2] = code1;
                }
                else
                {
                    if ((num2 & 0xff00) != 0xfe00)
                    {
                        throw new ArgumentException("Invalid OpCode.");
                    }
                    MultiByteOpCodes[num2 & 0xff] = code1;
                }
            }
        }
    }

    /// <summary>
    /// Retrieve the friendly name of a type.
    /// </summary>
    /// <param name="typeName">The complete name to the type.</param>
    /// <returns>
    /// The simplified name of the type (i.e. "int" instead f System.Int32).
    /// </returns>
    public static string ProcessSpecialTypes(string typeName)
    {
        var result = typeName;
        switch (typeName)
        {
            case "System.string":
            case "System.String":
            case "String":
                return "string";
            case "System.Int32":
            case "Int":
            case "Int32":
                return "int";
        }
        return result;
    }
}
