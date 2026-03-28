// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a command discovered from a loaded assembly.
/// </summary>
/// <param name="type">The command type.</param>
public class CommandDefinition(Type type)
{
    /// <summary>Gets the command type.</summary>
    public Type Type { get; } = type;

    /// <summary>Gets the command name (type name without namespace).</summary>
    public string Name => Type.Name;

    /// <summary>Gets the constructor parameters representing the command properties.</summary>
    public IReadOnlyList<ParameterInfo> Parameters =>
        Type.GetConstructors().FirstOrDefault()?.GetParameters() ?? [];
}

