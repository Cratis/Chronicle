// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Extension methods for preparing read model instances for client responses.
/// </summary>
internal static class ReadModelInstanceExtensions
{
    /// <summary>
    /// Excludes sink initialization state unless it is part of the read model's declared schema.
    /// </summary>
    /// <param name="instance">The sink-owned instance.</param>
    /// <param name="schema">The read model schema.</param>
    /// <returns>The response state, without mutating the sink-owned instance.</returns>
    internal static ExpandoObject WithoutInitializationState(this ExpandoObject instance, JsonSchema schema)
    {
        if (!((IDictionary<string, object?>)instance).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized) ||
            schema.GetFlattenedProperties().Any(property => property.Name == WellKnownProperties.ReadModelInstanceInitialized))
        {
            return instance;
        }

        // Initialization belongs to the projection pipeline, not the response. Keep the sink's instance intact.
        var result = new ExpandoObject();
        var values = (IDictionary<string, object?>)result;
        foreach (var (name, value) in instance.Where(property => property.Key != WellKnownProperties.ReadModelInstanceInitialized))
        {
            values[name] = value;
        }

        return result;
    }
}
