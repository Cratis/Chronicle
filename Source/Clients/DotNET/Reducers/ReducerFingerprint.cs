// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Produces a stable fingerprint for the executable parts of a reducer definition.
/// </summary>
/// <remarks>
/// Assembly versions, culture, keys and metadata tokens are excluded, not executable instructions or assembly
/// simple names. Anonymous types and delegates are identified structurally. Other compiler-generated names are
/// retained to distinguish closures and their captured fields. Changing compiler versions, optimization settings
/// or closure ordinals can still change the fingerprint. Bodies of external helpers are not hashed.
/// </remarks>
static class ReducerFingerprint
{
    const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Creates a fingerprint for a reducer type.
    /// </summary>
    /// <param name="reducerType">The reducer type.</param>
    /// <returns>A hexadecimal SHA-256 fingerprint.</returns>
    internal static string Create(Type reducerType)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var type in GetImplementationTypes(reducerType).OrderBy(GetTypeIdentity, StringComparer.Ordinal))
        {
            Append(hash, GetTypeIdentity(type));
            foreach (var contract in type.GetInterfaces().Select(GetTypeIdentity).Order(StringComparer.Ordinal))
            {
                Append(hash, contract);
            }

            // Include constructors, accessors, static helpers, lambdas and state-machine methods, not just handlers.
            foreach (var method in type.GetMethods(DeclaredMembers).Cast<MethodBase>()
                         .Concat(type.GetConstructors(DeclaredMembers))
                         .OrderBy(GetMemberIdentity, StringComparer.Ordinal))
            {
                Append(hash, GetMemberIdentity(method));
                ReducerILFingerprint.AppendBody(hash, method);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>
    /// Creates a conservative, build-specific fingerprint when IL normalization fails.
    /// </summary>
    /// <param name="reducerType">The reducer type.</param>
    /// <returns>The SHA-256 fingerprint.</returns>
    /// <remarks>The MVID identifies the entire built module, including IL and signatures we could not normalize.</remarks>
    internal static string CreateFallback(Type reducerType) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{reducerType.AssemblyQualifiedName}:{reducerType.Module.ModuleVersionId:D}")));

    /// <summary>
    /// Gets a recursive type identity with assembly simple names but without version or build metadata.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The namespace-qualified type identity.</returns>
    internal static string GetTypeIdentity(Type type)
    {
        if (type.IsGenericParameter) return $"{(type.DeclaringMethod is null ? "!" : "!!")}{type.GenericParameterPosition}";
        if (type.IsByRef) return $"{GetTypeIdentity(type.GetElementType()!)}&";
        if (type.IsPointer) return $"{GetTypeIdentity(type.GetElementType()!)}*";
        if (type.IsArray)
        {
            var dimensions = type.IsSZArray ? string.Empty : new string(',', type.GetArrayRank() - 1);
            if (!type.IsSZArray && type.GetArrayRank() == 1) dimensions = "*";
            return $"{GetTypeIdentity(type.GetElementType()!)}[{dimensions}]";
        }
        if (type.IsFunctionPointer)
        {
            return $"method:{(type.IsUnmanagedFunctionPointer ? "unmanaged" : "managed")}({string.Join(',', type.GetFunctionPointerCallingConventions().Select(GetTypeIdentity))})({string.Join(',', type.GetFunctionPointerParameterTypes().Select(GetTypeIdentity))}):{GetTypeIdentity(type.GetFunctionPointerReturnType())}";
        }
        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            if (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal))
            {
                // Preserve declaration order: property names, order and types define the anonymous shape.
                var properties = type.GetProperties(DeclaredMembers).OrderBy(_ => _.MetadataToken);
                return $"{type.Assembly.GetName().Name}::anonymous{{{string.Join(';', properties.Select(_ => $"{_.Name}:{GetTypeIdentity(_.PropertyType)}"))}}}";
            }
            if (type.Name.StartsWith("<>f__AnonymousDelegate", StringComparison.Ordinal) && type.BaseType == typeof(MulticastDelegate))
            {
                var invoke = type.GetMethod("Invoke")!;
                return $"{type.Assembly.GetName().Name}::anonymous-delegate({string.Join(',', invoke.GetParameters().Select(GetAnonymousDelegateParameterIdentity))}):{GetParameterIdentity(invoke.ReturnParameter)}:{invoke.CallingConvention}";
            }
        }
        if (type.IsGenericType)
        {
            return $"{GetNamedTypeIdentity(type.GetGenericTypeDefinition())}<{string.Join(',', type.GetGenericArguments().Select(GetTypeIdentity))}>";
        }

        return GetNamedTypeIdentity(type);
    }

    /// <summary>
    /// Gets the stable identity of a member referenced by IL, including its signature.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns>The member identity.</returns>
    internal static string GetMemberIdentity(MemberInfo member) => member switch
    {
        Type type => GetTypeIdentity(type),
        FieldInfo field => $"{GetTypeIdentity(field.DeclaringType!)}::{field.Name}:{GetTypeIdentity(field.FieldType)}",
        MethodBase method => $"{GetTypeIdentity(method.DeclaringType!)}::{method.Name}{GetGenericArguments(method)}({string.Join(',', method.GetParameters().Select(GetParameterIdentity))}):{(method is MethodInfo info ? GetParameterIdentity(info.ReturnParameter) : "void")}:{method.CallingConvention}",
        _ => member.Name
    };

    /// <summary>
    /// Appends a length-prefixed UTF-8 value to prevent ambiguous concatenations.
    /// </summary>
    /// <param name="hash">The destination hash.</param>
    /// <param name="value">The value to append.</param>
    internal static void Append(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Append(hash, bytes.Length);
        hash.AppendData(bytes);
    }

    /// <summary>
    /// Appends an integer in a fixed byte order, independent of culture and architecture.
    /// </summary>
    /// <param name="hash">The destination hash.</param>
    /// <param name="value">The value to append.</param>
    internal static void Append(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        hash.AppendData(bytes);
    }

    static string GetAnonymousDelegateParameterIdentity(ParameterInfo parameter)
    {
        var defaultValue = (parameter.HasDefaultValue, parameter.RawDefaultValue) switch
        {
            (false, _) => "none",
            (true, null) => "null",
            (_, var value) => $"value:{Convert.ToString(value, CultureInfo.InvariantCulture)}"
        };
        return $"{parameter.Name}:{parameter.Attributes}:{GetParameterIdentity(parameter)}:params({parameter.IsDefined(typeof(ParamArrayAttribute), false)}):default({defaultValue})";
    }

    static string GetNamedTypeIdentity(Type type) => $"{type.Assembly.GetName().Name}::{type.FullName ?? type.Name}";

    static string GetParameterIdentity(ParameterInfo parameter) =>
        $"{GetTypeIdentity(parameter.ParameterType)} modreq({string.Join(',', parameter.GetRequiredCustomModifiers().Select(GetTypeIdentity))}) modopt({string.Join(',', parameter.GetOptionalCustomModifiers().Select(GetTypeIdentity))})";

    static string GetGenericArguments(MethodBase method) => method.IsGenericMethod
        ? $"<{string.Join(',', method.GetGenericArguments().Select(GetTypeIdentity))}>"
        : string.Empty;

    static IEnumerable<Type> GetImplementationTypes(Type type)
    {
        yield return type;
        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(_ => _.IsDefined(typeof(CompilerGeneratedAttribute), false))
                     .SelectMany(GetImplementationTypes))
        {
            yield return nested;
        }
    }
}
