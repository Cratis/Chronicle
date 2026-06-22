// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;

/// <summary>
/// Marks the removal of an item using an empty event that relies on the implicit EventSourceId.
/// </summary>
[EventType]
public record ItemRemovedWithEmpty;
