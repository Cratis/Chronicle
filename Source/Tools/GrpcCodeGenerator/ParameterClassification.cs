// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Decides whether a parameter on a query method carries wire data or is resolved from the service container.
/// </summary>
/// <remarks>
/// Both generators have to agree on this. The interface generator uses it to decide which parameters become
/// properties on the request message; the implementation generator uses it to decide which arguments come off
/// the request and which come off the class' own dependencies. A disagreement produces an implementation that
/// does not satisfy the interface it claims to implement, so the rule lives in one place.
/// </remarks>
public static class ParameterClassification
{
    static readonly HashSet<string> _collectionInterfaces =
    [
        "IEnumerable`1",
        "ICollection`1",
        "IList`1",
        "IReadOnlyCollection`1",
        "IReadOnlyList`1",
        "ISet`1",
        "IReadOnlySet`1",
        "IDictionary`2",
        "IReadOnlyDictionary`2"
    ];

    /// <summary>
    /// Full names of well-known framework types that are DI-registered singletons even though they are concrete,
    /// sealed classes rather than interfaces - System.Text.Json.JsonSerializerOptions is the one this generator's
    /// own artifacts already take as a query parameter.
    /// </summary>
    /// <remarks>
    /// Treating it as wire data instead of a dependency does not fail loudly: protobuf-net.Grpc's SchemaGenerator
    /// silently drops the RPC method (and its request/response messages) it cannot model a field for, rather than
    /// throwing, so the method simply never reaches the wire. See
    /// <c>Cratis.Chronicle.Sequences.AppendedEvent.QueryEvents</c> and its siblings.
    /// <para>
    /// Matched by full name, not by <c>typeof()</c> equality - the artifact assembly is loaded via reflection from
    /// a path rather than referenced, so its copy of System.Text.Json is not guaranteed to be the same
    /// <see cref="Type"/> instance this generator's own compiled reference to the same assembly would produce.
    /// </para>
    /// </remarks>
    static readonly HashSet<string> _knownDependencyTypeNames = ["System.Text.Json.JsonSerializerOptions"];

    /// <summary>
    /// Determines whether a parameter is resolved from the service container rather than carried on the wire.
    /// </summary>
    /// <param name="type">The parameter type.</param>
    /// <returns>True when the parameter is a dependency.</returns>
    /// <remarks>
    /// An interface or an abstract class is a service. A collection interface is not - it is how a query declares
    /// that it takes many of something, and treating it as a dependency silently drops the parameter from the
    /// request message and from the wire.
    /// </remarks>
    public static bool IsDependency(Type type)
    {
        if (IsCollection(type))
        {
            return false;
        }

        return type.IsInterface || type.IsAbstract || _knownDependencyTypeNames.Contains(type.FullName ?? string.Empty);
    }

    /// <summary>
    /// Determines whether a type is a collection shape rather than a service.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a collection.</returns>
    public static bool IsCollection(Type type)
    {
        if (type.IsArray)
        {
            return true;
        }

        return type.IsGenericType && _collectionInterfaces.Contains(type.GetGenericTypeDefinition().Name);
    }
}
