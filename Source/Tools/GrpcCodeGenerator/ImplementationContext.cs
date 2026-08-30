// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Carries the state shared while generating one service implementation.
/// </summary>
/// <param name="contractsNamespace">The namespace the generated contracts for this service live in.</param>
public sealed class ImplementationContext(string contractsNamespace)
{
    readonly Dictionary<Type, ResponseMapping> _mappings = [];

    /// <summary>
    /// Gets the namespace the generated contracts for this service live in.
    /// </summary>
    public string ContractsNamespace { get; } = contractsNamespace;

    /// <summary>
    /// Gets the services the implementation has to be constructed with.
    /// </summary>
    public ImplementationDependencies Dependencies { get; } = new();

    /// <summary>
    /// Gets the mappings that have to be emitted alongside the service methods.
    /// </summary>
    public IReadOnlyCollection<ResponseMapping> Mappings => _mappings.Values;

    /// <summary>
    /// Gets the mapping onto a read model's generated response message, registering it the first time it is needed.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The mapping.</returns>
    /// <remarks>
    /// A read model's message is built from its constructor parameters - the same source the interface generator
    /// builds the message from, so the two agree on the member set by construction.
    /// </remarks>
    public ResponseMapping MappingForReadModel(Type readModelType) =>
        MappingFor(
            readModelType,
            $"{readModelType.Name}Response",
            () =>
            {
                var constructor = readModelType.GetConstructors().FirstOrDefault()
                    ?? throw new UnsupportedServiceShape(readModelType.FullName ?? readModelType.Name, "it has no constructor to read its members from.");

                return [.. constructor.GetParameters().Select(p => (ImplementationValues.PropertyName(p.Name ?? "value"), p.ParameterType, MemberNullability.Of(p)))];
            });

    /// <summary>
    /// Gets the mapping onto a command's generated response message, registering it the first time it is needed.
    /// </summary>
    /// <param name="responseType">The type the command's handler responds with.</param>
    /// <param name="commandName">The name of the command.</param>
    /// <returns>The mapping.</returns>
    public ResponseMapping MappingForCommandResponse(Type responseType, string commandName) =>
        MappingFor(
            responseType,
            $"{commandName}Response",
            () => [.. responseType.GetProperties()
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(p => (p.Name, p.PropertyType, MemberNullability.Of(p)))]);

    ResponseMapping MappingFor(Type domainType, string contractTypeName, Func<IReadOnlyList<(string Name, Type Type, bool IsNullable)>> members)
    {
        if (_mappings.TryGetValue(domainType, out var existing))
        {
            return existing;
        }

        var mapping = new ResponseMapping(
            domainType,
            $"global::{ContractsNamespace}.{contractTypeName}",
            $"To{contractTypeName}",
            members());

        _mappings[domainType] = mapping;
        return mapping;
    }
}
