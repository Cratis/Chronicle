// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// A recorded brief was removed from its parent.
/// </summary>
/// <param name="Id">The identifier of the removed brief.</param>
[EventType]
public record PathBriefRemoved(Guid Id);
