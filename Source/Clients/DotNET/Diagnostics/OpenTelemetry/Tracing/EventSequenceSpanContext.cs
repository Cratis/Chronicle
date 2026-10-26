// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>The routing attributes of an event sequence span.</summary>
/// <param name="EventStoreName">The event store.</param>
/// <param name="NamespaceName">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceType">The event source type, when appending one event.</param>
internal record EventSequenceSpanContext(string EventStoreName, string NamespaceName, string EventSequenceId, string? EventSourceType = null);
