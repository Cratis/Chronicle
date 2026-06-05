// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsCompliance"/> that applies and releases
/// PII compliance for read model instances via the <see cref="IJsonComplianceManager"/>.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for encrypting and decrypting PII fields.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
public class ReadModelsCompliance(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IReadModelsCompliance
{
    /// <inheritdoc/>
    public async Task<ExpandoObject> Apply(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        string identifier,
        ExpandoObject instance)
    {
        if (!schema.HasComplianceMetadata())
        {
            ((IDictionary<string, object?>)instance)[WellKnownProperties.Subject] = identifier;
            return instance;
        }

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        var applied = await complianceManager.Apply(eventStore, eventStoreNamespace, schema, identifier, json);
        var result = expandoObjectConverter.ToExpandoObject(applied, schema);
        ((IDictionary<string, object?>)result)[WellKnownProperties.Subject] = identifier;
        return result;
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ReleaseJson(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        JsonObject instance)
    {
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        if (instance[WellKnownProperties.Subject]?.GetValue<string>() is not string identifier)
        {
            return instance;
        }

        return await complianceManager.Release(eventStore, eventStoreNamespace, schema, identifier, instance);
    }

    /// <inheritdoc/>
    public async Task<ExpandoObject> Release(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        ExpandoObject instance)
    {
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        var dict = (IDictionary<string, object?>)instance;
        if (!dict.TryGetValue(WellKnownProperties.Subject, out var subjectObj) || subjectObj is null)
        {
            return instance;
        }

        var identifier = subjectObj.ToString();
        if (string.IsNullOrEmpty(identifier))
        {
            return instance;
        }

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        var released = await complianceManager.Release(eventStore, eventStoreNamespace, schema, identifier, json);
        return expandoObjectConverter.ToExpandoObject(released, schema);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExpandoObject>> Release(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        IEnumerable<ExpandoObject> instances)
    {
        var result = new List<ExpandoObject>();
        foreach (var instance in instances)
        {
            result.Add(await Release(eventStore, eventStoreNamespace, schema, instance));
        }

        return result;
    }
}
