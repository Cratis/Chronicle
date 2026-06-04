// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelComplianceHelper"/> for releasing (decrypting) PII-annotated properties in read model instances.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
/// <param name="expandoObjectConverter">The expando object converter.</param>
public class ReadModelReleaseHelper(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IReadModelComplianceHelper
{
    /// <inheritdoc/>
    public async Task<ExpandoObject> Release(
        string eventStore,
        string @namespace,
        JsonSchema schema,
        ExpandoObject instance)
    {
        return await ReadModelComplianceHelper.Release(
            complianceManager,
            eventStore,
            @namespace,
            schema,
            instance,
            expandoObjectConverter);
    }

    /// <inheritdoc/>
    public async Task<IList<ExpandoObject>> Release(
        string eventStore,
        string @namespace,
        JsonSchema schema,
        IEnumerable<ExpandoObject> instances)
    {
        var result = new List<ExpandoObject>();
        foreach (var instance in instances)
        {
            var released = await Release(
                eventStore,
                @namespace,
                schema,
                instance);
            result.Add(released);
        }
        return result;
    }
}
