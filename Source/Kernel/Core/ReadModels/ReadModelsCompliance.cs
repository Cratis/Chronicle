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
        var resultAsDictionary = (IDictionary<string, object?>)result;

        // Encryption must only change the PII members; it must not drop the rest of the document. The
        // schema round-trip above only carries schema-declared properties, so document identity and
        // bookkeeping fields that live outside the read model schema — the sink's primary key column and
        // similar — are lost. Downstream difference computation would then see them as removed and emit a
        // spurious "property -> null" change; for the SQL sink that nulls the read model's primary key and
        // the save fails on every attempt. Carry any such non-schema property through unchanged.
        foreach (var (propertyName, propertyValue) in (IDictionary<string, object?>)instance)
        {
            if (!resultAsDictionary.ContainsKey(propertyName))
            {
                resultAsDictionary[propertyName] = propertyValue;
            }
        }

        resultAsDictionary[WellKnownProperties.Subject] = identifier;
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

        // Kernel bookkeeping is stamped onto the document by the kernel itself — the identity marker read above,
        // the sink's last-handled watermark, the projection engine's initialization flag. The compliance manager
        // walks every property it is handed against the read model schema and rejects anything the schema does not
        // declare, so any of them left on fails the whole release. The ExpandoObject release paths get that for
        // free from their schema round-trip, which carries only schema-declared properties; this path has no
        // round-trip and has to be explicit. The invariant is that the manager receives exactly the schema's
        // document, so strip the whole set — stripping only the marker this method happens to read leaves the next
        // property stamped upstream to reintroduce the same failure.
        //
        // Which of them come off is decided against the schema's own flattened properties, the same lookup the
        // compliance walk does: a read model is free to expose a bookkeeping property as its own, and several
        // declare __lastHandledEventSequenceNumber, in which case it is a property like any other and has to
        // survive. Strip on a copy — the caller keeps the document it passed in.
        var declaredByTheSchema = schema.GetFlattenedProperties().Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var withoutBookkeeping = (instance.DeepClone() as JsonObject)!;
        foreach (var property in WellKnownProperties.All.Where(_ => !declaredByTheSchema.Contains(_)))
        {
            withoutBookkeeping.Remove(property);
        }

        return await complianceManager.Release(eventStore, eventStoreNamespace, schema, identifier, withoutBookkeeping);
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
