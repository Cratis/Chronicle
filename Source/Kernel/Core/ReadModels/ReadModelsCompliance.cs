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

        var instanceAsDictionary = (IDictionary<string, object?>)instance;
        var defaultSubject = instanceAsDictionary.TryGetValue(WellKnownProperties.Subject, out var storedSubject) &&
                             storedSubject?.ToString() is { Length: > 0 } storedSubjectValue
            ? storedSubjectValue
            : identifier;
        var subjects = instanceAsDictionary.TryGetValue(WellKnownProperties.Subjects, out var storedSubjects)
            ? ReadModelSubjects.From(storedSubjects)
            : [];

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        var applied = await HandleBySubject(
            json,
            defaultSubject,
            subjects,
            (subject, slice) => complianceManager.Apply(eventStore, eventStoreNamespace, schema, subject, slice));
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

        resultAsDictionary[WellKnownProperties.Subject] = defaultSubject;
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

        var identifier = instance[WellKnownProperties.Subject]?.GetValue<string>();
        var subjects = ReadModelSubjects.From(instance[WellKnownProperties.Subjects]);
        if (identifier is null && subjects.Count == 0)
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

        return await HandleBySubject(
            withoutBookkeeping,
            identifier,
            subjects,
            (subject, slice) => complianceManager.Release(eventStore, eventStoreNamespace, schema, subject, slice));
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
        var identifier = dict.TryGetValue(WellKnownProperties.Subject, out var subjectObj)
            ? subjectObj?.ToString()
            : null;
        var subjects = dict.TryGetValue(WellKnownProperties.Subjects, out var subjectsObj)
            ? ReadModelSubjects.From(subjectsObj)
            : [];
        if (string.IsNullOrEmpty(identifier) && subjects.Count == 0)
        {
            return instance;
        }

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        var released = await HandleBySubject(
            json,
            identifier,
            subjects,
            (subject, slice) => complianceManager.Release(eventStore, eventStoreNamespace, schema, subject, slice));
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

    static async Task<JsonObject> HandleBySubject(
        JsonObject json,
        string? defaultSubject,
        Dictionary<string, string> subjects,
        Func<string, JsonObject, Task<JsonObject>> action)
    {
        if (subjects.Count == 0)
        {
            return string.IsNullOrEmpty(defaultSubject)
                ? json
                : await action(defaultSubject, json);
        }

        var result = (json.DeepClone() as JsonObject)!;
        var groups = json
            .Select(property => new
            {
                property.Key,
                Subject = subjects.TryGetValue(property.Key, out var subject) ? subject : defaultSubject
            })
            .Where(_ => !string.IsNullOrEmpty(_.Subject))
            .GroupBy(_ => _.Subject!, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var slice = new JsonObject();
            foreach (var property in group)
            {
                slice[property.Key] = json[property.Key]?.DeepClone();
            }

            var handled = await action(group.Key, slice);
            foreach (var (property, value) in handled)
            {
                result[property] = value?.DeepClone();
            }
        }

        return result;
    }
}
