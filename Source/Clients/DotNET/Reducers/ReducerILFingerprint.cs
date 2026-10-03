// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using static Cratis.Chronicle.Reducers.ReducerFingerprint;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Hashes executable IL with metadata references resolved in the declaring method's generic context.
/// </summary>
static class ReducerILFingerprint
{
    static readonly Dictionary<short, OpCode> _opCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(_ => _.FieldType == typeof(OpCode))
        .Select(_ => (OpCode)_.GetValue(null)!)
        .ToDictionary(_ => _.Value);

    /// <summary>
    /// Appends normalized instructions, locals and exception regions for a method.
    /// </summary>
    /// <param name="hash">The destination hash.</param>
    /// <param name="method">The method to fingerprint.</param>
    internal static void AppendBody(IncrementalHash hash, MethodBase method)
    {
        var body = method.GetMethodBody();
        Append(hash, body is null ? 0 : 1);
        if (body is null) return;

        Append(hash, body.InitLocals ? 1 : 0);
        Append(hash, body.LocalVariables.Count);
        foreach (var local in body.LocalVariables)
        {
            Append(hash, GetTypeIdentity(local.LocalType));
            Append(hash, local.IsPinned ? 1 : 0);
        }

        Append(hash, body.ExceptionHandlingClauses.Count);
        foreach (var clause in body.ExceptionHandlingClauses)
        {
            Append(hash, (int)clause.Flags);
            Append(hash, clause.TryOffset);
            Append(hash, clause.TryLength);
            Append(hash, clause.HandlerOffset);
            Append(hash, clause.HandlerLength);
            if (clause.Flags == ExceptionHandlingClauseOptions.Clause) Append(hash, GetTypeIdentity(clause.CatchType!));
            if (clause.Flags == ExceptionHandlingClauseOptions.Filter) Append(hash, clause.FilterOffset);
        }

        var bytes = body.GetILAsByteArray()!;
        Append(hash, bytes.Length);
        var offset = 0;
        while (offset < bytes.Length)
        {
            var value = (short)bytes[offset++];
            if (value == 0xfe) value = (short)(0xfe00 | bytes[offset++]);
            var opCode = _opCodes[value];
            Append(hash, value);
            switch (opCode.OperandType)
            {
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    var member = method.Module.ResolveMember(ReadToken(bytes, ref offset), method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null)!;
                    Append(hash, GetMemberIdentity(member));
                    break;
                case OperandType.InlineString:
                    Append(hash, method.Module.ResolveString(ReadToken(bytes, ref offset)));
                    break;
                case OperandType.InlineSig:
                    ReducerSignatureFingerprint.AppendSignature(hash, method, method.Module.ResolveSignature(ReadToken(bytes, ref offset)));
                    break;
                default:
                    // Constants, local/argument indices and relative branch/switch offsets contain no metadata tokens.
                    var size = GetOperandSize(opCode.OperandType, bytes, offset);
                    hash.AppendData(bytes.AsSpan(offset, size));
                    offset += size;
                    break;
            }
        }
    }

    static int ReadToken(byte[] bytes, ref int offset)
    {
        var token = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
        offset += sizeof(int);
        return token;
    }

    static int GetOperandSize(OperandType operandType, byte[] bytes, int offset) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget or OperandType.InlineI or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => sizeof(int) * (1 + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset))),
        _ => throw new UnsupportedReducerFingerprintOperand(operandType.ToString())
    };
}
