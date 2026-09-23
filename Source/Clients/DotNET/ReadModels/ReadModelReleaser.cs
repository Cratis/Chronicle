// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Runs the release pass over read model instances - decrypting both compliance (<c language="csharp">[PII]</c>) and
/// security (<c language="csharp">[Encrypted]</c>) fields, via the kernel's generalized schema metadata handling.
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
    /// Release the compliance- and security-annotated values on a read model instance.
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
        if (!schema.HasSchemaMetadata())
        {
            return instance;
        }

        return await ReleaseAgainst(schema, instance);
    }

    /// <summary>
    /// Release the compliance- and security-annotated values on a sequence of read model instances.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="instances">The instances to release.</param>
    /// <returns>The released instances.</returns>
    /// <remarks>
    /// Whether a read model has anything to release is a property of its type, not of any one instance, so the schema
    /// is resolved and asked once for the whole sequence. The overwhelmingly common case - a read model that carries no
    /// schema metadata at all - then costs one lookup rather than one per instance, and the sequence is handed back
    /// untouched instead of being copied into a new list only to hold the same references.
    /// </remarks>
    public async Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances)
    {
        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasSchemaMetadata())
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
    /// Release a single instance against a schema already known to carry schema metadata.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model to release.</typeparam>
    /// <param name="schema">The schema describing the instance.</param>
    /// <param name="instance">The instance to release.</param>
    /// <returns>The released instance, or the original when it names no subject to release against.</returns>
    /// <remarks>
    /// A subject-scoped value - <c language="csharp">[PII]</c>, or <c language="csharp">[Encrypted]</c> with the default
    /// <c language="csharp">EncryptionScope.Subject</c> - is keyed by a resolved subject, so when no subject resolves
    /// there is nothing for it to have been encrypted under, and skipping the call is correct (this is also the
    /// shape of a computed <c language="csharp">[PII]</c> value that was never round-tripped through encryption at
    /// all). A namespace- or global-scoped <c language="csharp">[Encrypted]</c> value is keyed independently of any
    /// subject, so it needs releasing regardless - and the read model carrying only that kind of value ordinarily
    /// has no <c language="csharp">Id</c> or <c language="csharp">[Subject]</c> at all, since there is no per-document
    /// identity for it to be scoped by. <see cref="SecurityJsonSchemaExtensions.HasSubjectIndependentSecurityMetadata"/>
    /// is what tells those two cases apart: only when the schema carries subject-independent security metadata does a
    /// failed subject resolution still fall back to <see cref="Subject.NotSet"/> rather than skip. The kernel already
    /// degrades a subject-scoped property it cannot resolve a key for to an empty value rather than failing the whole
    /// release (see <c language="csharp">JsonSchemaMetadataManager</c>), so <see cref="Subject.NotSet"/> is safe there
    /// even when the schema also carries subject-scoped metadata the instance genuinely has no subject for.
    /// </remarks>
    async Task<TReadModel> ReleaseAgainst<TReadModel>(JsonSchema schema, TReadModel instance)
    {
        var subject = ReadModelSubjectResolver.ResolveFrom(instance);
        if (subject is null)
        {
            if (!schema.HasSubjectIndependentSecurityMetadata())
            {
                logger.NoSubjectForRelease(typeof(TReadModel).Name);
                return instance;
            }

            logger.NoResolvableSubjectFallingBackToNotSet(typeof(TReadModel).Name);
            subject = Subject.NotSet;
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
