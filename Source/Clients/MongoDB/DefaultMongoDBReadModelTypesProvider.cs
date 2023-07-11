// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.DependencyInversion;
using Aksio.Types;
using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ICanProvideMongoDBReadModelTypes"/> that uses <see cref="ProjectReferencedAssemblies"/> for providing read model types.
/// </summary>
public class DefaultMongoDBReadModelTypesProvider : ICanProvideMongoDBReadModelTypes
{
    readonly IEnumerable<Type> _types;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMongoDBReadModelTypesProvider"/> class.
    /// </summary>
    /// <param name="assembliesProvider"><see cref="ICanProvideAssembliesForDiscovery"/> for assemblies that holds types that can be used to discover read models from.</param>
    public DefaultMongoDBReadModelTypesProvider(ICanProvideAssembliesForDiscovery assembliesProvider)
    {
        var types = assembliesProvider.DefinedTypes;
        var mongoCollections = GetFromMongoCollectionDependencies(types);
        var providers = GetFromProvidersForMongoCollectionDependencies(types, mongoCollections);
        _types = mongoCollections.Concat(providers);
    }

    /// <inheritdoc/>
    public IEnumerable<Type> Provide() => _types;

    static IEnumerable<Type> GetFromMongoCollectionDependencies(IEnumerable<Type> types) => types.SelectMany(_ => _
            .GetConstructors().SelectMany(c => c.GetParameters())
            .Where(_ =>
                _.ParameterType.IsGenericType && IsMongoCollection(_.ParameterType)))
            .Select(_ => _.ParameterType.GetGenericArguments()[0]);

    static IEnumerable<Type> GetFromProvidersForMongoCollectionDependencies(IEnumerable<Type> types, IEnumerable<Type> typesToSkip) => types.Except(typesToSkip).SelectMany(_ => _
            .GetConstructors().SelectMany(c => c.GetParameters())
            .Where(_ =>
                _.ParameterType.IsGenericType &&
                _.ParameterType.GetGenericArguments()[0].IsGenericType &&
                IsProviderForMongoCollection(_.ParameterType)))
            .Select(_ => _.ParameterType.GetGenericArguments()[0].GetGenericArguments()[0]);

    static bool IsMongoCollection(Type type) => type.IsAssignableTo(typeof(IMongoCollection<>).MakeGenericType(type.GetGenericArguments()[0]));

    static bool IsProviderForMongoCollection(Type type) => type.IsAssignableTo(typeof(ProviderFor<>).MakeGenericType(typeof(IMongoCollection<>).MakeGenericType(type.GetGenericArguments()[0].GetGenericArguments()[0])));
}
