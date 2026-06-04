// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Implementation of <see cref="IReadModelsCompliance"/> that delegates to the static <see cref="ReadModelComplianceHelper"/> methods for PII release operations.
/// This class serves as a bridge between the injectable service pattern and the underlying compliance infrastructure.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
/// <param name="expandoObjectConverter">The expando object converter.</param>
public class ReadModelsCompliance(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IReadModelsCompliance
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
