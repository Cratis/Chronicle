// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a <c>[Query]</c>-marked method on a <c>[KeyedBy&lt;TKey&gt;]</c> grain interface.
/// </summary>
/// <param name="grainInterfaceType">The grain interface the method is declared on.</param>
/// <param name="keyType">The type declared by the interface's <c>[KeyedBy&lt;TKey&gt;]</c> attribute.</param>
/// <param name="method">The method info representing the query.</param>
public class KeyedQueryDefinition(Type grainInterfaceType, Type keyType, MethodInfo method)
{
    /// <summary>Gets the grain interface the method is declared on.</summary>
    public Type GrainInterfaceType { get; } = grainInterfaceType;

    /// <summary>Gets the type whose constructor parameters make up the grain key.</summary>
    public Type KeyType { get; } = keyType;

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

    /// <summary>
    /// Gets the key type's constructor parameters, in declaration order - the fields <see cref="KeyType"/>'s own
    /// <c>ToString()</c> is expected to combine into the grain key, and the fields the generated request carries so
    /// the implementation can reconstruct that same key with <c>KeyHelper.Combine</c>.
    /// </summary>
    public IReadOnlyList<ParameterInfo> KeyParameters => KeyType.GetConstructors()[0].GetParameters();
}
