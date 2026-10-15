// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// The author edited a draft.
/// </summary>
/// <param name="Revision">The revision.</param>
[EventType]
public record DraftEdited(int Revision);
