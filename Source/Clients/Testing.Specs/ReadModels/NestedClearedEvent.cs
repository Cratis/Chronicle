// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>The event that clears the inner object.</summary>
/// <param name="Key">The read model key.</param>
[EventType]
public record NestedClearedEvent(string Key);
