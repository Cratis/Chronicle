// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Coordinates plaintext migrations and generation-specific protection at the persistence boundary.
/// </summary>
/// <param name="eventTypes">Storage for generation schemas.</param>
/// <param name="migrations">The plaintext migration engine.</param>
/// <param name="metadataManager">The metadata manager releasing and protecting event values.</param>
/// <param name="converter">The schema-guided content converter.</param>
internal class ProtectedEventTypeMigrations(
    IEventTypesStorage eventTypes,
    IEventTypeMigrations migrations,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter)
{
    /// <summary>
    /// Releases stored event content before migrating it and protecting target generations.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="eventType">The source event type and generation.</param>
    /// <param name="protectedContent">The stored JSON content.</param>
    /// <param name="protectedEvent">The stored event content.</param>
    /// <param name="subject">The original event's compliance subject.</param>
    /// <returns>Protected content for every generation.</returns>
    internal async Task<IDictionary<EventTypeGeneration, ExpandoObject>> Migrate(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventType eventType,
        JsonObject protectedContent,
        ExpandoObject protectedEvent,
        string subject)
    {
        var sourceSchema = await eventTypes.GetFor(eventType.Id, eventType.Generation);
        var plaintext = sourceSchema.Schema.HasSchemaMetadata()
            ? await metadataManager.ReleaseStrict(eventStore, @namespace, sourceSchema.Schema, subject, protectedContent)
            : protectedContent;
        return await MigratePlaintext(eventStore, @namespace, eventType, plaintext, protectedEvent, subject, storedContent: true);
    }

    /// <summary>
    /// Migrates plaintext append content and protects target generations.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="eventType">The source event type and generation.</param>
    /// <param name="plaintext">The original plaintext JSON content.</param>
    /// <param name="protectedEvent">The already-protected source generation.</param>
    /// <param name="subject">The original event's compliance subject.</param>
    /// <param name="storedContent">Whether the source is already stored and must honor prior subject erasure.</param>
    /// <returns>Protected content for every generation.</returns>
    internal async Task<IDictionary<EventTypeGeneration, ExpandoObject>> MigratePlaintext(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventType eventType,
        JsonObject plaintext,
        ExpandoObject protectedEvent,
        string subject,
        bool storedContent = false)
    {
        var sourceSchema = await eventTypes.GetFor(eventType.Id, eventType.Generation);
        var plaintextEvent = converter.ToExpandoObject(plaintext, sourceSchema.Schema);
        var generations = await migrations.MigrateToAllGenerations(eventStore, eventType, plaintext, plaintextEvent);
        var protectedGenerations = new Dictionary<EventTypeGeneration, ExpandoObject>();
        foreach (var (generation, content) in generations)
        {
            // The source generation is already protected. Never encrypt it a second time.
            if (generation == eventType.Generation)
            {
                protectedGenerations[generation] = protectedEvent;
                continue;
            }

            var schema = await eventTypes.GetFor(eventType.Id, generation);
            if (!schema.Schema.HasSchemaMetadata())
            {
                protectedGenerations[generation] = content;
                continue;
            }

            var json = converter.ToJsonObject(content, schema.Schema);
            var applied = storedContent
                ? await metadataManager.ApplyToReadModel(eventStore, @namespace, schema.Schema, subject, json)
                : await metadataManager.Apply(eventStore, @namespace, schema.Schema, subject, json);
            protectedGenerations[generation] = converter.ToExpandoObject(applied, schema.Schema);
        }

        return protectedGenerations;
    }
}
