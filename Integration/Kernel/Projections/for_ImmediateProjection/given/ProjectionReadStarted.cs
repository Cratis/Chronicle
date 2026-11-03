// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Provides an event for the immediate projection to fold.
/// </summary>
/// <param name="Name">The projected name.</param>
[EventType("e0bb5403-a160-4f5b-b966-327f27730921")]
public record ProjectionReadStarted(string Name);
