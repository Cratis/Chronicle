// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of what removes an element in a child relationship.
/// </summary>
/// <param name="Event">The event that is causing the removal.</param>
public record RemovedWithDefinition(EventType Event);
