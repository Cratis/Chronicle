// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;

namespace Aksio.Cratis.Kernel.Domain.Events;

/// <summary>
/// Payload for registering multiple event types.
/// </summary>
/// <param name="Types">Collection of <see cref="EventTypeRegistration"/>.</param>
public record RegisterEventTypes(IEnumerable<EventTypeRegistration> Types);
