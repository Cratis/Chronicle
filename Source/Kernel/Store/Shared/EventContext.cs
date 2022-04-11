// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Represents the context in which an event exists - typically what it was appended with.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/>.</param>
/// <param name="Occurred"><see cref="DateTimeOffset">When</see> it occurred.</param>
/// <param name="ValidFrom"><see cref="DateTimeOffset">When</see> event is considered valid from.</param>
/// <param name="TenantId">The <see cref="TenantId"/> the event was appended to.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the event.</param>
/// <param name="CausationId">The <see cref="CausationId"/> for what caused the event.</param>
/// <param name="CausedBy">Identity of what caused the event.</param>
public record EventContext(EventSourceId EventSourceId, DateTimeOffset Occurred, DateTimeOffset ValidFrom, TenantId TenantId, CorrelationId CorrelationId, CausationId CausationId, CausedBy CausedBy);
