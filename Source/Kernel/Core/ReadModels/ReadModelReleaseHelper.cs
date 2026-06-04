// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Helper for releasing (decrypting) PII-annotated properties in read model instances.
/// </summary>
public static class ReadModelReleaseHelper
{
    /// <summary>
    /// Release (decrypt) PII-annotated properties in a single read model instance.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="schema">The read model schema.</param>
    /// <param name="instance">The read model instance to decrypt.</param>
    /// <param name="expandoObjectConverter">The expando object converter.</param>
    /// <returns>The decrypted instance, or the original when release is not applicable or fails.</returns>
    public static async Task<ExpandoObject> Release(
        IJsonComplianceManager complianceManager,
        string eventStore,
        string @namespace,
        JsonSchema schema,
        ExpandoObject instance,
        IExpandoObjectConverter expandoObjectConverter)
    {
        return await ReadModelComplianceHelper.Release(
            complianceManager,
            eventStore,
            @namespace,
            schema,
            instance,
            expandoObjectConverter);
    }

    /// <summary>
    /// Release (decrypt) PII-annotated properties in a collection of read model instances.
    /// </summary>
    /// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="schema">The read model schema.</param>
    /// <param name="instances">The collection of instances to decrypt.</param>
    /// <param name="expandoObjectConverter">The expando object converter.</param>
    /// <returns>The decrypted instances.</returns>
    public static async Task<List<ExpandoObject>> Release(
        IJsonComplianceManager complianceManager,
        string eventStore,
        string @namespace,
        JsonSchema schema,
        IEnumerable<ExpandoObject> instances,
        IExpandoObjectConverter expandoObjectConverter)
    {
        var result = new List<ExpandoObject>();
        foreach (var instance in instances)
        {
            var released = await Release(
                complianceManager,
                eventStore,
                @namespace,
                schema,
                instance,
                expandoObjectConverter);
            result.Add(released);
        }
        return result;
    }
}
