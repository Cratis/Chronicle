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

    /// <summary>
    /// Gets the type the command's Handle method responds with, or null when the command produces no response.
    /// </summary>
    /// <remarks>
    /// A command that responds with a value needs that value carried across the wire, so the generated
    /// operation returns <c>CommandResult&lt;TResponse&gt;</c> rather than a bare <c>CommandResult</c>.
    /// </remarks>
    public Type? ResponseType => ResolveResponseType();

    Type? ResolveResponseType()
    {
        var handle = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(method => method.Name == "Handle");

        if (handle is null)
        {
            return null;
        }

        var returnType = handle.ReturnType;
        if (returnType == typeof(void) || returnType == typeof(Task))
        {
            return null;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType == typeof(void) ? null : returnType;
    }
}

