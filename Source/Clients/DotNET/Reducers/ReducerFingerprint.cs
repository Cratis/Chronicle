// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Produces a stable fingerprint for the executable parts of a reducer definition.
/// </summary>
static class ReducerFingerprint
{
    /// <summary>
    /// Creates a fingerprint for a reducer type.
    /// </summary>
    /// <param name="reducerType">The reducer type.</param>
    /// <returns>A hexadecimal SHA-256 fingerprint.</returns>
    internal static string Create(Type reducerType)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var method in reducerType
                     .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(method => !method.IsSpecialName)
                     .OrderBy(GetSignature, StringComparer.Ordinal))
        {
            Append(hash, GetSignature(method));
            AppendMethodBody(hash, method);

            var stateMachine = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            var moveNext = stateMachine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (moveNext is not null)
            {
                AppendMethodBody(hash, moveNext);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    static string GetSignature(MethodInfo method) =>
        $"{method.Name}({string.Join(',', method.GetParameters().Select(parameter => parameter.ParameterType.AssemblyQualifiedName))}):{method.ReturnType.AssemblyQualifiedName}";

    static void AppendMethodBody(IncrementalHash hash, MethodInfo method)
    {
        var body = method.GetMethodBody()?.GetILAsByteArray();
        if (body is not null)
        {
            hash.AppendData(body);
        }
    }

    static void Append(IncrementalHash hash, string value) => hash.AppendData(Encoding.UTF8.GetBytes(value));
}
