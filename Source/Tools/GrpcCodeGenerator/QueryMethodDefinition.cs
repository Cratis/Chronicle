// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a query method on a read model type.
/// </summary>
/// <param name="method">The method info representing the query.</param>
public class QueryMethodDefinition(MethodInfo method)
{
    /// <summary>Gets the underlying method info.</summary>
    public MethodInfo Method { get; } = method;

    /// <summary>Gets the method name.</summary>
    public string Name => Method.Name;

    /// <summary>Gets the method parameters.</summary>
    public IReadOnlyList<ParameterInfo> Parameters => Method.GetParameters();

    /// <summary>Gets the return type of the method.</summary>
    public Type ReturnType => Method.ReturnType;

    /// <summary>Gets whether this is an observable query (returns ISubject or IObservable).</summary>
    public bool IsObservable => TypeHelper.IsObservableType(Method.ReturnType);
}
