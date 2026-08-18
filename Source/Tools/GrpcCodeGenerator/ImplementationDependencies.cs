// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Collects the services a generated implementation has to be constructed with, and names each one.
/// </summary>
/// <remarks>
/// A generated implementation dispatches to every command and query on its service, so it needs the union of
/// everything those artifacts ask for. Registration order is the order the artifacts are visited, which keeps
/// the emitted primary constructor stable between runs for an unchanged assembly.
/// </remarks>
public sealed class ImplementationDependencies
{
    readonly Dictionary<Type, string> _names = [];
    readonly List<(Type Type, string Name)> _ordered = [];

    /// <summary>
    /// Gets the dependencies in the order they were first seen.
    /// </summary>
    public IReadOnlyList<(Type Type, string Name)> All => _ordered;

    /// <summary>
    /// Gets the name to refer to a dependency by, registering it the first time it is seen.
    /// </summary>
    /// <param name="type">The dependency type.</param>
    /// <returns>The parameter name.</returns>
    public string NameFor(Type type)
    {
        if (_names.TryGetValue(type, out var existing))
        {
            return existing;
        }

        var name = Unique(BaseNameFor(type));
        _names[type] = name;
        _ordered.Add((type, name));
        return name;
    }

    /// <summary>
    /// Derives a readable parameter name from a type.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The camel-cased name.</returns>
    static string BaseNameFor(Type type)
    {
        var builder = new StringBuilder(Camel(Simple(type)));

        if (type.IsGenericType)
        {
            builder.Append("Of");
            foreach (var argument in type.GetGenericArguments())
            {
                builder.Append(Simple(argument));
            }
        }

        return builder.ToString();
    }

    static string Simple(Type type)
    {
        var name = type.Name;
        var backtick = name.IndexOf('`');
        if (backtick >= 0)
        {
            name = name[..backtick];
        }

        // An interface's leading I is noise in a parameter name - IStorage is injected as storage.
        if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
        {
            name = name[1..];
        }

        return name;
    }

    static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    string Unique(string candidate)
    {
        if (!_names.ContainsValue(candidate))
        {
            return candidate;
        }

        var index = 2;
        while (_names.ContainsValue($"{candidate}{index}"))
        {
            index++;
        }

        return $"{candidate}{index}";
    }
}
