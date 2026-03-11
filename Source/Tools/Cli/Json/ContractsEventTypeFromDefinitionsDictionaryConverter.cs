// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Json;

namespace Cratis.Chronicle.Cli.Json;

/// <summary>
/// Converts a dictionary of <see cref="EventType"/> and <see cref="FromDefinition"/> to and from JSON.
/// </summary>
public class ContractsEventTypeFromDefinitionsDictionaryConverter : DictionaryJsonConverter<EventType, FromDefinition>
{
    /// <inheritdoc/>
    protected override EventType GetKeyFromString(string key) => new() { Id = key, Generation = 1 };

    /// <inheritdoc/>
    protected override string GetKeyString(EventType key) => key.Id;
}
