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

        var subject = ReadModelSubjectResolver.ResolveFrom(instance);

        return subject is null ? instance : await ReleaseWhole(subject, instance);
    }

    /// <summary>
    /// Release the compliance-annotated values on a sequence of read model instances.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="instances">The instances to release.</param>
    /// <returns>The released instances.</returns>
    public async Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances)
    {
        var result = new List<TReadModel>();
        foreach (var instance in instances)
        {
            result.Add(await Release(instance));
        }

        return result;
    }

    async Task<TReadModel> ReleaseWhole<TReadModel>(Subject subject, TReadModel instance)
    {
        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

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
