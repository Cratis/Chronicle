// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event whose <see cref="TallyReducer"/> handler throws, used to verify reducer failures surface.
/// </summary>
[EventType]
public record TallyBroke;
