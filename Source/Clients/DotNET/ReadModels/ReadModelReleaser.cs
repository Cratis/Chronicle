// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Runs the compliance release pass over read model instances.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> the read models belong to.</param>
/// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for describing the payload.</param>
/// <param name="servicesAccessor">The <see cref="IChronicleServicesAccessor"/> for reaching the kernel.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> the payload round-trips through.</param>
/// <param name="logger">The <see cref="ILogger"/> for diagnostics.</param>
internal class ReadModelReleaser(
    IEventStore eventStore,
    IJsonSchemaGenerator schemaGenerator,
    IChronicleServicesAccessor servicesAccessor,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger logger)
{
    /// <summary>
    /// Release the compliance-annotated values on a read model instance.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="instance">The instance to release.</param>
    /// <returns>The released instance, or the original when there is nothing to release.</returns>
    public async Task<TReadModel> Release<TReadModel>(TReadModel instance)
    {
        if (instance is null)
        {
            return instance;
        }

        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        return await ReleaseAgainst(schema, instance);
    }

    /// <summary>
    /// Release the compliance-annotated values on a sequence of read model instances.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="instances">The instances to release.</param>
    /// <returns>The released instances.</returns>
    /// <remarks>
    /// Whether a read model has anything to release is a property of its type, not of any one instance, so the schema
    /// is resolved and asked once for the whole sequence. The overwhelmingly common case - a read model that carries no
    /// compliance metadata at all - then costs one lookup rather than one per instance, and the sequence is handed back
    /// untouched instead of being copied into a new list only to hold the same references.
    /// </remarks>
    public async Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances)
    {
        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasComplianceMetadata())
        {
            return instances;
        }

        var result = new List<TReadModel>();
        foreach (var instance in instances)
        {
            result.Add(instance is null ? instance : await ReleaseAgainst(schema, instance));
        }

        return result;
    }

    /// <summary>
    /// Release a single instance against a schema already known to carry compliance metadata.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="schema">The schema describing the instance.</param>
    /// <param name="instance">The instance to release.</param>
    /// <returns>The released instance, or the original when it names no subject to release against.</returns>
    async Task<TReadModel> ReleaseAgainst<TReadModel>(JsonSchema schema, TReadModel instance)
    {
        var subject = ReadModelSubjectResolver.ResolveFrom(instance);
        if (subject is null)
        {
            logger.NoSubjectForRelease(typeof(TReadModel).Name);
            return instance;
        }

        return await ReleaseWhole(subject, instance, schema);
    }

    async Task<TReadModel> ReleaseWhole<TReadModel>(Subject subject, TReadModel instance, JsonSchema schema)
    {
        var payload = JsonSerializer.Serialize(instance, jsonSerializerOptions);
        var released = await ReleasePayload<TReadModel>(subject, schema.ToJson(), payload);

        return released is null
            ? instance
            : JsonSerializer.Deserialize<TReadModel>(released, jsonSerializerOptions) ?? instance;
    }

    async Task<string?> ReleasePayload<TReadModel>(Subject subject, string schemaJson, string payload)
    {
        var response = await servicesAccessor.Services.Compliance.Release(new ReleaseRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            Subject = subject.Value,
            Schema = schemaJson,
            Payload = payload
        });

        if (!response.HasError)
        {
            return response.Payload;
        }

        logger.FailedToRelease(typeof(TReadModel).Name, subject.Value, response.Error);
        return null;
    }
}
