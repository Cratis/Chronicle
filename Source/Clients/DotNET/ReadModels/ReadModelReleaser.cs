// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Runs the compliance release pass over read model instances, honoring any per-property
/// <see cref="Compliance.GDPR.SubjectFromAttribute"/> declarations the read model carries.
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

        var plan = ReadModelReleasePlan.For(typeof(TReadModel));
        var subject = ReadModelSubjectResolver.ResolveFrom(instance);

        if (!plan.HasDeclarations)
        {
            // Byte for byte the behavior every read model had before per-property declarations existed:
            // no subject, nothing happens; a subject, one call carrying the whole payload.
            return subject is null ? instance : await ReleaseWhole(subject, instance);
        }

        return await ReleaseByDeclaration(plan, subject, instance);
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

    static JsonObject Slice(JsonObject payload, IEnumerable<string> keys)
    {
        var slice = new JsonObject();
        foreach (var key in keys.Where(payload.ContainsKey))
        {
            slice[key] = payload[key]?.DeepClone();
        }

        return slice;
    }

    static void Merge(JsonObject payload, JsonObject released)
    {
        foreach (var (key, value) in released)
        {
            payload[key] = value?.DeepClone();
        }
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

    async Task<TReadModel> ReleaseByDeclaration<TReadModel>(ReadModelReleasePlan plan, Subject? ownSubject, TReadModel instance)
    {
        var schema = schemaGenerator.Generate(typeof(TReadModel));
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        if (JsonSerializer.SerializeToNode(instance, jsonSerializerOptions) is not JsonObject payload)
        {
            return instance;
        }

        var schemaJson = schema.ToJson();
        var declaredKeys = new HashSet<string>(StringComparer.Ordinal);
        var anyReleased = false;

        foreach (var group in plan.Groups)
        {
            var keys = group.Properties.Select(property => KeyFor(payload, property)).OfType<string>().ToArray();
            declaredKeys.UnionWith(keys);

            var subject = ReadModelSubjectResolver.ToSubject(group.SubjectProperty.GetValue(instance));
            if (subject is null)
            {
                logger.DeclaredReleaseSubjectNotResolved(
                    typeof(TReadModel).Name,
                    string.Join("', '", group.Properties.Select(property => property.Name)),
                    group.SubjectProperty.Name);
                continue;
            }

            anyReleased |= await ReleaseInto<TReadModel>(payload, keys, subject, schemaJson);
        }

        // Everything the read model did not speak for keeps releasing under the read model's own subject —
        // the undeclared half behaves exactly as it always has, including doing nothing when none resolves.
        if (ownSubject is not null)
        {
            anyReleased |= await ReleaseInto<TReadModel>(payload, payload.Select(entry => entry.Key).Except(declaredKeys, StringComparer.Ordinal), ownSubject, schemaJson);
        }

        return anyReleased
            ? JsonSerializer.Deserialize<TReadModel>(payload.ToJsonString(jsonSerializerOptions), jsonSerializerOptions) ?? instance
            : instance;
    }

    async Task<bool> ReleaseInto<TReadModel>(JsonObject payload, IEnumerable<string> keys, Subject subject, string schemaJson)
    {
        var slice = Slice(payload, keys);
        if (slice.Count == 0)
        {
            return false;
        }

        var released = await ReleasePayload<TReadModel>(subject, schemaJson, slice.ToJsonString(jsonSerializerOptions));
        if (released is null || JsonNode.Parse(released) is not JsonObject releasedSlice)
        {
            return false;
        }

        Merge(payload, releasedSlice);
        return true;
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

    string? KeyFor(JsonObject payload, PropertyInfo property)
    {
        var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                   jsonSerializerOptions.PropertyNamingPolicy?.ConvertName(property.Name) ??
                   property.Name;
        if (payload.ContainsKey(name))
        {
            return name;
        }

        // Match the effective serialized name with the serializer's configured case behavior.
        var comparison = jsonSerializerOptions.PropertyNameCaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return payload
            .Select(entry => entry.Key)
            .FirstOrDefault(key => string.Equals(key, name, comparison));
    }
}
