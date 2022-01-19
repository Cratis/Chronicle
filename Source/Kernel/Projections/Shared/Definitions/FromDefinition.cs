// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Represents the definition from for a specific event.
    /// </summary>
    /// <param name="Properties">Properties and expressions for each property.</param>
    /// <param name="Key">Optional key expression, represents the key to use for identifying the model instance. If not specified; eventSourceId will be used.</param>
    /// <param name="ParentKey">Optional parent key expression, typically used in child relationships for identifying parent model.</param>
    public record FromDefinition(IDictionary<PropertyPath, string> Properties, string? Key, string? ParentKey);
}
