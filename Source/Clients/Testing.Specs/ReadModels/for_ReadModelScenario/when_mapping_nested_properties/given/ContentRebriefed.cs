// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

/// <summary>
/// A new brief replaced the content's original topic.
/// </summary>
/// <param name="Brief">The replacement brief.</param>
[EventType]
public record ContentRebriefed(Brief Brief);
