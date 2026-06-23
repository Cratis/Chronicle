// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;

/// <summary>
/// Marks the creation of an item that can be removed with an empty event.
/// </summary>
/// <param name="Name">The name of the item.</param>
[EventType]
public record ItemDefinedWithEmptyRemoval(string Name);
