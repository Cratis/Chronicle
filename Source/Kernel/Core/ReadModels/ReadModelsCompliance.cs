// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
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
            (subject, slice) => complianceManager.ApplyToReadModel(eventStore, eventStoreNamespace, schema, subject, slice));
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
        if (!schema.HasSchemaMetadata())
        {
            return instance;
        }

        var identifier = instance[WellKnownProperties.Subject]?.GetValue<string>();
        var subjects = ReadModelSubjects.From(instance[WellKnownProperties.Subjects]);
        return await ReleaseJson(eventStore, eventStoreNamespace, schema, instance, identifier, subjects);
    }

    /// <inheritdoc/>
    public async Task<JsonObject> ReleaseJson(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        JsonObject instance,
        string? subject,
        IReadOnlyDictionary<string, string> subjects)
    {
        if (!schema.HasSchemaMetadata())
        {
            return instance;
        }

        if (subject is null && subjects.Count == 0)
        {
            return instance;
        }

        // The manager walks every property it is handed against the read model schema and rejects anything the schema
        // does not declare, so kernel bookkeeping is taken off first (Cratis/Chronicle#3585). Callers that go through
        // ReadModelChangeBookkeeping.Split have already done it; the strip is kept as a defensive guard.
        var withoutBookkeeping = ReadModelChangeBookkeeping.WithoutUndeclaredBookkeeping(instance, schema);
        return await HandleBySubject(
            withoutBookkeeping,
            subject,
            subjects.ToDictionary(_ => _.Key, _ => _.Value, StringComparer.Ordinal),
            (owner, slice) => complianceManager.Release(eventStore, eventStoreNamespace, schema, owner, slice));
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
        using var scope = ReleaseKeys.BeginScope();
        var result = new List<ExpandoObject>();
        foreach (var instance in instances)
        {
            result.Add(await Release(eventStore, eventStoreNamespace, schema, instance));
        }

        return await ReleaseKeys.Complete<IEnumerable<ExpandoObject>>(() => result);
    }

    static async Task<JsonObject> HandleBySubject(
        JsonObject json,
        string? defaultSubject,
        Dictionary<string, string> subjects,
        Func<string, JsonObject, Task<JsonObject>> action)
    {
        using var scope = ReleaseKeys.BeginScope();
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

        return await ReleaseKeys.Complete(() => result);
    }
}
