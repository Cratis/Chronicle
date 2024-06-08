// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Aksio.Cratis.Properties;
using Aksio.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Json;

/// <summary>
/// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="PropertyPath"/> and <see cref="JoinDefinition"/>.
/// </summary>
public class JoinDefinitionsConverter : DictionaryJsonConverter<EventType, JoinDefinition>
{
    /// <inheritdoc/>
    protected override EventType GetKeyFromString(string key) => new(key, 1);

    /// <inheritdoc/>
    protected override string GetKeyString(EventType key) => key.Id.ToString();
}
