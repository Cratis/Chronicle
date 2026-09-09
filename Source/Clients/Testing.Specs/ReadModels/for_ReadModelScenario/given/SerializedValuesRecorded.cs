// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Records ordinary values together with a property whose wire name differs from its CLR name.
/// </summary>
/// <param name="Name">The scalar name.</param>
/// <param name="Count">The scalar count.</param>
/// <param name="Code">The concept value.</param>
/// <param name="Labels">The dictionary values.</param>
/// <param name="ClrName">The value serialized under a different property name.</param>
[EventType]
public record SerializedValuesRecorded(
    string Name,
    int Count,
    ConceptListItem Code,
    IReadOnlyDictionary<string, string> Labels,
    [property: JsonPropertyName("serializedName")] string ClrName);
