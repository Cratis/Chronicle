// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Projections.Json;

/// <summary>
/// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="PropertyPath"/> and <see cref="JoinDefinition"/>.
/// </summary>
public class JoinDefinitionsConverter : DictionaryJsonConverter<EventType, JoinDefinition>
{
    /// <inheritdoc/>
    protected override EventType GetKeyFromString(string key) => new(key, EventGeneration.First, false);

    /// <inheritdoc/>
    protected override string GetKeyString(EventType key) => key.Id.ToString();
}
