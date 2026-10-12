// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// Content was briefed by its author.
/// </summary>
/// <param name="Brief">The brief.</param>
/// <param name="Author">The author.</param>
[EventType]
public record ContentBriefed(Brief Brief, string Author);
