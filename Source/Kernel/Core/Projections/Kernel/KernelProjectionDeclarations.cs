// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Lowers kernel projection declarations into the definitions the projection engine already understands.
/// </summary>
/// <remarks>
/// Kept separate from the registration that consumes it. Everything that can be got wrong about a declaration -
/// which read model it targets, what identifier it lands under, how a dual scope becomes two definitions - happens
/// here, where it can be specified against a type rather than against a cluster.
/// </remarks>
public static class KernelProjectionDeclarations
{
    /// <summary>
    /// Find every kernel projection declaration among a set of types.
    /// </summary>
    /// <param name="types">The types to look through.</param>
    /// <returns>The declaring types.</returns>
    public static IEnumerable<Type> Discover(IEnumerable<Type> types) =>
        types.Where(type => !type.IsAbstract && !type.IsInterface && ReadModelTypeFor(type) is not null);

    /// <summary>
    /// Gets the read model a type declares a kernel projection for, or null when it declares none.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The read model type, or null.</returns>
    public static Type? ReadModelTypeFor(Type type) =>
        type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKernelProjectionFor<>))
            ?.GetGenericArguments()[0];

    /// <summary>
    /// Lowers a declaration into the read model and projection definitions the engine understands.
    /// </summary>
    /// <param name="declaration">The declaring type.</param>
    /// <returns>The read model definition and the projection definitions that write to it.</returns>
    /// <exception cref="KernelProjectionWithoutIdentifier">Thrown when the declaration carries no <see cref="KernelProjectionAttribute"/>.</exception>
    /// <remarks>
    /// A <see cref="ProjectionScope.Both"/> declaration lowers to two projection definitions against one read
    /// model - the global half carries its own identifier suffix, and its sink is resolved at event store level
    /// rather than per namespace, so the two never write to the same place.
    /// </remarks>
    public static (ReadModelDefinition ReadModel, IReadOnlyCollection<ProjectionDefinition> Projections) Lower(Type declaration)
    {
        var readModelType = ReadModelTypeFor(declaration) ?? throw new KernelProjectionWithoutIdentifier(declaration);
        var attribute = declaration.GetCustomAttribute<KernelProjectionAttribute>() ?? throw new KernelProjectionWithoutIdentifier(declaration);

        var identifier = WellKnownKernelProjections.IdentifierFor(attribute.Id);
        var readModelIdentifier = new ReadModelIdentifier(readModelType.Name);

        var builderType = typeof(KernelProjectionBuilder<>).MakeGenericType(readModelType);
        var builder = Activator.CreateInstance(builderType, identifier, readModelIdentifier)!;

        var instance = Activator.CreateInstance(declaration)!;
        declaration
            .GetInterfaceMap(typeof(IKernelProjectionFor<>).MakeGenericType(readModelType))
            .TargetMethods
            .Single(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(IKernelProjectionBuilder<>).MakeGenericType(readModelType))
            .Invoke(instance, [builder]);

        var definitions = (IReadOnlyCollection<ProjectionDefinition>)builderType
            .GetMethod(nameof(KernelProjectionBuilder<object>.Build))!
            .Invoke(builder, [])!;

        return (ReadModelDefinitionFor(readModelType, readModelIdentifier, identifier), definitions);
    }

    /// <summary>
    /// Builds the read model definition a declaration's projections write to.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="identifier">The <see cref="ReadModelIdentifier"/>.</param>
    /// <param name="projection">The <see cref="ProjectionId"/> that writes it.</param>
    /// <returns>The <see cref="ReadModelDefinition"/>.</returns>
    /// <remarks>
    /// The schema is generated from the CLR type rather than authored, which is the point of declaring these in
    /// the kernel at all - the read model and the thing that describes it cannot drift apart.
    /// </remarks>
    static ReadModelDefinition ReadModelDefinitionFor(Type readModelType, ReadModelIdentifier identifier, ProjectionId projection)
    {
        var schema = JsonSchema.FromType(readModelType);
        schema.Title = readModelType.Name;

        return new ReadModelDefinition(
            identifier,
            readModelType.Name,
            readModelType.Name,
            ReadModelOwner.Server,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            projection.Value,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
            []);
    }
}
