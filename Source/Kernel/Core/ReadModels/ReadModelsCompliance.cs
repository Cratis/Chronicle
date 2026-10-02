// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsCompliance"/> that applies and releases both
/// compliance (<c language="csharp">[PII]</c>) and security (<c language="csharp">[Encrypted]</c>) protection for read model instances
/// via the <see cref="IJsonSchemaMetadataManager"/>.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonSchemaMetadataManager"/> for encrypting and decrypting the protected fields.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
public class ReadModelsCompliance(
    IJsonSchemaMetadataManager complianceManager,
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
        if (!schema.HasSchemaMetadata())
        {
            ((IDictionary<string, object?>)instance)[WellKnownProperties.Subject] = identifier;
            return instance;
        }
        schema.EnsureProtectionCanBeResolved();

        var instanceAsDictionary = (IDictionary<string, object?>)instance;
        var defaultSubject = instanceAsDictionary.TryGetValue(WellKnownProperties.Subject, out var storedSubject) &&
                             storedSubject?.ToString() is { Length: > 0 } storedSubjectValue
            ? storedSubjectValue
            : identifier;
        var subjects = instanceAsDictionary.TryGetValue(WellKnownProperties.Subjects, out var storedSubjects)
            ? ReadModelSubjects.From(storedSubjects)
            : [];

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        PreserveInputNulls(instance, json, schema);
        var unprotectedConversionLosses = schema.IsUnprotectedSchemaValue(includeMembers: false)
            ? schema.GetFlattenedProperties()
                .GroupBy(property => property.Name, StringComparer.Ordinal)
                .Where(group => !json.ContainsKey(group.Key) && instanceAsDictionary.ContainsKey(group.Key) &&
                    group.All(property => property.IsUnprotectedSchemaValue()))
                .ToDictionary(group => group.Key, group => instanceAsDictionary[group.Key], StringComparer.Ordinal)
            : [];
        var applied = await HandleBySubject(
            schema,
            json,
            defaultSubject,
            subjects,
            (subject, slice) => complianceManager.ApplyToReadModel(eventStore, eventStoreNamespace, schema, subject, slice));
        var result = expandoObjectConverter.ToExpandoObject(applied, schema);
        var resultAsDictionary = (IDictionary<string, object?>)result;

        // The compliance result is authoritative, including null erasure placeholders. Only losses classified
        // as unprotected before the walk may use the original; never overwrite a converted or protected value.
        PreserveOutputNulls(applied, result);
        foreach (var (property, value) in unprotectedConversionLosses.Where(_ => !resultAsDictionary.ContainsKey(_.Key)))
        {
            resultAsDictionary[property] = value;
        }
        var declaredProperties = schema.GetFlattenedProperties().Select(_ => _.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (property, value) in instanceAsDictionary.Where(_ => !declaredProperties.Contains(_.Key) && !resultAsDictionary.ContainsKey(_.Key)))
        {
            // The sink key and other undeclared state must survive the conversion, but an alternate casing of
            // a declared member is not undeclared state and must never bypass that member's protection.
            if (!schema.IsUnprotectedSchemaValue(includeMembers: false) && !WellKnownProperties.All.Contains(property, StringComparer.Ordinal))
            {
                throw new UnresolvedSchemaProtection($"undeclared member of a protected container: {property}");
            }
            resultAsDictionary[property] = value;
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
        if (!schema.HasSchemaMetadata())
        {
            return instance;
        }
        schema.EnsureProtectionCanBeResolved();

        var identifier = instance[WellKnownProperties.Subject]?.GetValue<string>();
        var subjects = ReadModelSubjects.From(instance[WellKnownProperties.Subjects]);

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
            schema,
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
        if (!schema.HasSchemaMetadata())
        {
            return instance;
        }
        schema.EnsureProtectionCanBeResolved();

        var dict = (IDictionary<string, object?>)instance;
        var identifier = dict.TryGetValue(WellKnownProperties.Subject, out var subjectObj)
            ? subjectObj?.ToString()
            : null;
        var subjects = dict.TryGetValue(WellKnownProperties.Subjects, out var subjectsObj)
            ? ReadModelSubjects.From(subjectsObj)
            : [];

        var json = expandoObjectConverter.ToJsonObject(instance, schema);
        PreserveInputNulls(instance, json, schema);
        var released = await HandleBySubject(
            schema,
            json,
            identifier,
            subjects,
            (subject, slice) => complianceManager.Release(eventStore, eventStoreNamespace, schema, subject, slice));
        var result = expandoObjectConverter.ToExpandoObject(released, schema);
        PreserveOutputNulls(released, result);
        return result;
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

    static void PreserveInputNulls(object? original, JsonNode? converted, JsonSchema schema)
    {
        if (original is ExpandoObject source && converted is JsonObject result)
        {
            var values = (IDictionary<string, object?>)source;
            foreach (var property in schema.GetFlattenedProperties().DistinctBy(_ => _.Name))
            {
                if (!values.TryGetValue(property.Name, out var value))
                {
                    var matching = values.FirstOrDefault(_ => _.Key.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                    if (matching.Key is null) continue;
                    value = matching.Value;
                }

                if (value is null && !result.ContainsKey(property.Name))
                {
                    result[property.Name] = null;
                }
                else
                {
                    PreserveInputNulls(value, result[property.Name], property);
                }
            }
        }
        else if (original is IEnumerable sourceItems and not string && converted is JsonArray resultItems &&
                 (schema.Item ?? schema.ActualTypeSchema.Item) is { } itemSchema)
        {
            var items = sourceItems.Cast<object?>().ToArray();
            for (var index = 0; index < Math.Min(items.Length, resultItems.Count); index++)
            {
                PreserveInputNulls(items[index], resultItems[index], itemSchema);
            }
        }
    }

    static void PreserveOutputNulls(JsonNode? handled, object? converted)
    {
        if (handled is JsonObject source && converted is ExpandoObject result)
        {
            var values = (IDictionary<string, object?>)result;
            foreach (var (name, value) in source)
            {
                if (value is null)
                {
                    values[name] = null;
                }
                else if (values.TryGetValue(name, out var current))
                {
                    PreserveOutputNulls(value, current);
                }
            }
        }
        else if (handled is JsonArray sourceItems && converted is object?[] resultItems)
        {
            for (var index = 0; index < Math.Min(sourceItems.Count, resultItems.Length); index++)
            {
                PreserveOutputNulls(sourceItems[index], resultItems[index]);
            }
        }
    }

    static async Task<JsonObject> HandleBySubject(
        JsonSchema schema,
        JsonObject json,
        string? defaultSubject,
        Dictionary<string, string> subjects,
        Func<string, JsonObject, Task<JsonObject>> action)
    {
        var resolved = schema.ResolveComposition();
        var protectsContainer = !resolved.IsUnprotectedSchemaValue(includeMembers: false);
        foreach (var property in json.Where(property => string.IsNullOrEmpty(subjects.TryGetValue(property.Key, out var subject) ? subject : defaultSubject)))
        {
            if (protectsContainer || resolved.Properties.Any(member => member.Key.Equals(property.Key, StringComparison.OrdinalIgnoreCase) && !member.Value.IsUnprotectedSchemaValue()))
            {
                throw new UnresolvedSchemaProtection($"missing subject for {property.Key}");
            }
        }

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
