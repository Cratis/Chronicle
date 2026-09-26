// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>The event that reestablishes the nested model.</summary>
/// <param name="Key">The read model key.</param>
/// <param name="Name">The name.</param>
[EventType]
public record NestedRegistered(string Key, string Name);
