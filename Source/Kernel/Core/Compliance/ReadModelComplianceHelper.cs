// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Provides compliance apply/release operations for read model <see cref="ExpandoObject"/> instances.
/// </summary>
public static class ReadModelComplianceHelper
{
    /// <summary>
    /// Encrypts PII fields in a read model instance and writes the compliance subject.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> to use for applying compliance.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read model's properties.</param>
    /// <param name="identifier">The compliance subject identifier used as the encryption key reference.</param>
    /// <param name="instance">The <see cref="ExpandoObject"/> read model instance to encrypt.</param>
    /// <param name="converter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
    /// <returns>A new <see cref="ExpandoObject"/> with PII fields encrypted and <c>_subject</c> written.</returns>
    public static async Task<ExpandoObject> Apply(
        IJsonComplianceManager complianceManager,
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        string identifier,
        ExpandoObject instance,
        IExpandoObjectConverter converter)
    {
        if (!schema.HasComplianceMetadata())
        {
            ((IDictionary<string, object?>)instance)[WellKnownProperties.Subject] = identifier;
            return instance;
        }

        var json = converter.ToJsonObject(instance, schema);
        var applied = await complianceManager.Apply(eventStore, eventStoreNamespace, schema, identifier, json);
        var result = converter.ToExpandoObject(applied, schema);
        ((IDictionary<string, object?>)result)[WellKnownProperties.Subject] = identifier;
        return result;
    }

    /// <summary>
    /// Decrypts PII fields in a read model instance using the stored <c>_subject</c> field.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> to use for releasing compliance.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the read model's properties.</param>
    /// <param name="instance">The <see cref="ExpandoObject"/> read model instance to decrypt.</param>
    /// <param name="converter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
    /// <returns>A new <see cref="ExpandoObject"/> with PII fields decrypted, or the original instance if no subject is stored.</returns>
    public static async Task<ExpandoObject> Release(
        IJsonComplianceManager complianceManager,
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        JsonSchema schema,
        ExpandoObject instance,
        IExpandoObjectConverter converter)
    {
        if (!schema.HasComplianceMetadata())
        {
            return instance;
        }

        var dict = (IDictionary<string, object?>)instance;
        if (!dict.TryGetValue(WellKnownProperties.Subject, out var subjectObj) || subjectObj is not string identifier)
        {
            return instance;
        }

        var json = converter.ToJsonObject(instance, schema);
        var released = await complianceManager.Release(eventStore, eventStoreNamespace, schema, identifier, json);
        return converter.ToExpandoObject(released, schema);
    }
}
