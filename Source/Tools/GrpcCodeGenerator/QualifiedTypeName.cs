// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Renders a CLR type as a fully qualified C# type name.
/// </summary>
/// <remarks>
/// The generated implementations sit in a namespace that shares leading segments with the artifacts they
/// dispatch to and with the contracts they implement, so an unqualified name is ambiguous more often than not -
/// <c>Jobs</c> alone can mean the generated class, the artifact namespace, or the contract namespace. Every
/// type reference the generator emits for a domain type is therefore <c>global::</c>-qualified.
/// </remarks>
public static class QualifiedTypeName
{
    static readonly Dictionary<Type, string> _keywords = new()
    {
        [typeof(void)] = "void",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(object)] = "object"
    };

    /// <summary>
    /// Gets the fully qualified C# name for a type.
    /// </summary>
    /// <param name="type">The type to render.</param>
    /// <returns>The fully qualified name.</returns>
    public static string For(Type type)
    {
        if (_keywords.TryGetValue(type, out var keyword))
        {
            return keyword;
        }

        if (type.IsArray)
        {
            return $"{For(type.GetElementType()!)}[]";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return $"{For(type.GetGenericArguments()[0])}?";
        }

        var builder = new StringBuilder("global::");

        if (!string.IsNullOrEmpty(type.Namespace))
        {
            builder.Append(type.Namespace).Append('.');
        }

        builder.Append(NameWithoutArity(type));

        if (type.IsGenericType)
        {
            builder
                .Append('<')
                .AppendJoin(", ", type.GetGenericArguments().Select(For))
                .Append('>');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets a type's name without its generic arity suffix, with nested types joined by a dot.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The name.</returns>
    static string NameWithoutArity(Type type)
    {
        var names = new List<string>();
        var current = type;
        while (current is not null)
        {
            var name = current.Name;
            var backtick = name.IndexOf('`');
            names.Insert(0, backtick >= 0 ? name[..backtick] : name);
            current = current.DeclaringType;
        }

        return string.Join('.', names);
    }
}
