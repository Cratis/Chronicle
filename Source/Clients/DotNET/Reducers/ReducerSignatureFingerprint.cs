// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Cryptography;
using static Cratis.Chronicle.Reducers.ReducerFingerprint;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Normalizes standalone call-site signatures (calli), whose type references are compressed metadata tokens.
/// </summary>
static class ReducerSignatureFingerprint
{
    /// <summary>
    /// Appends a call-site signature with its embedded type references resolved.
    /// </summary>
    /// <param name="hash">The destination hash.</param>
    /// <param name="method">The method providing the generic and module context.</param>
    /// <param name="signature">The ECMA-335 standalone method signature.</param>
    /// <exception cref="UnsupportedReducerFingerprintOperand">The signature contains an unsupported element or type reference.</exception>
    internal static void AppendSignature(IncrementalHash hash, MethodBase method, byte[] signature)
    {
        var offset = 0;
        AppendMethodSignature();

        // ECMA-335 II.23.2: method signatures and their recursively encoded types.
        void AppendMethodSignature()
        {
            var convention = signature[offset++];
            Append(hash, convention);
            if ((convention & 0x10) != 0) AppendNumber();
            var parameters = AppendNumber();
            AppendType();
            for (var index = 0; index < parameters; index++)
            {
                if (signature[offset] == 0x41) Append(hash, signature[offset++]); // Vararg sentinel.
                AppendType();
            }
        }

        void AppendType()
        {
            var element = signature[offset++];
            Append(hash, element);
            switch (element)
            {
                case >= 0x01 and <= 0x0e: // Primitive types, including void and string.
                case 0x16: // Typed reference.
                case 0x18: // Native int.
                case 0x19: // Native unsigned int.
                case 0x1c: // Object.
                    break;
                case 0x0f: // Pointer.
                case 0x10: // By reference.
                case 0x1d: // SZArray.
                case 0x45: // Pinned.
                    AppendType();
                    break;
                case 0x11: // Value type.
                case 0x12: // Class.
                    AppendTypeReference();
                    break;
                case 0x13: // Generic type parameter.
                case 0x1e: // Generic method parameter.
                    AppendNumber();
                    break;
                case 0x14: // Multidimensional array: element, rank, sizes and lower bounds.
                    AppendType();
                    AppendNumber();
                    var sizes = AppendNumber();
                    for (var index = 0; index < sizes; index++) AppendNumber();
                    var bounds = AppendNumber();
                    for (var index = 0; index < bounds; index++) AppendNumber();
                    break;
                case 0x15: // Generic instantiation.
                    AppendType();
                    var arguments = AppendNumber();
                    for (var index = 0; index < arguments; index++) AppendType();
                    break;
                case 0x1b: // Function pointer.
                    AppendMethodSignature();
                    break;
                case 0x1f: // Required custom modifier.
                case 0x20: // Optional custom modifier.
                    AppendTypeReference();
                    AppendType();
                    break;
                default:
                    throw new UnsupportedReducerFingerprintOperand($"signature element {element:X2}");
            }
        }

        void AppendTypeReference()
        {
            var encoded = ReadNumber();
            var table = (encoded & 3) switch
            {
                0 => 0x02000000,
                1 => 0x01000000,
                2 => 0x1b000000,
                _ => throw new UnsupportedReducerFingerprintOperand("signature type reference")
            };
            var type = method.Module.ResolveType(table | (encoded >> 2), method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
            Append(hash, GetTypeIdentity(type));
        }

        int AppendNumber()
        {
            var value = ReadNumber();
            Append(hash, value);
            return value;
        }

        int ReadNumber()
        {
            var first = signature[offset++];
            if ((first & 0x80) == 0) return first;
            if ((first & 0xc0) == 0x80) return ((first & 0x3f) << 8) | signature[offset++];
            return ((first & 0x1f) << 24) | (signature[offset++] << 16) | (signature[offset++] << 8) | signature[offset++];
        }
    }
}
