// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.XUnit.Events;
using Cratis.Json;
using Cratis.Types;

namespace Cratis.Chronicle.XUnit;

/// <summary>
/// Represents default implementations for Chronicle services.
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Gets the default <see cref="IEventStore"/>.
    /// </summary>
    public static readonly IEventStore EventStore;

    /// <summary>
    /// Gets the default <see cref="IEventTypes"/>.
    /// </summary>
    public static readonly IEventTypes EventTypes;

    /// <summary>
    /// Gets the default <see cref="IJsonSchemaGenerator"/>.
    /// </summary>
    public static readonly IJsonSchemaGenerator JsonSchemaGenerator;

    /// <summary>
    /// Gets the default <see cref="IClientArtifactsProvider"/>.
    /// </summary>
    public static readonly IClientArtifactsProvider ClientArtifactsProvider;

    /// <summary>
    /// Gets the default <see cref="IEventSerializer"/>.
    /// </summary>
    public static readonly IEventSerializer EventSerializer;

    static Defaults()
    {
        EventStore = new EventStoreForTesting();
        JsonSchemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));

        ClientArtifactsProvider = new DefaultClientArtifactsProvider(
            new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));
        ClientArtifactsProvider.Initialize();

        EventTypes = new EventTypes(
            EventStore,
            JsonSchemaGenerator,
            ClientArtifactsProvider);

        EventTypes.Discover().Wait();

        EventSerializer = new EventSerializer(
            ClientArtifactsProvider,
            new DefaultServiceProvider(),
            EventTypes,
            Globals.JsonSerializerOptions);
    }
}
