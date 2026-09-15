// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Projects scalar, concept and dictionary values using the event's serialized property names.
/// </summary>
/// <param name="Id">The read model identifier.</param>
/// <param name="Name">The scalar name.</param>
/// <param name="Count">The scalar count.</param>
/// <param name="Code">The concept value.</param>
/// <param name="Labels">The dictionary values.</param>
/// <param name="SerializedName">The value mapped from the event's wire property name.</param>
[Passive]
[FromEvent<SerializedValuesRecorded>]
public record SerializedValuesReadModel(
    Guid Id,
    string Name,
    int Count,
    ConceptListItem Code,
    IReadOnlyDictionary<string, string> Labels,
    string SerializedName);
