// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Telemetry.when_replaying_with_default_orleans_tracing;

[EventType("4607-replay-event-applied")]
public record EventApplied(int Value);
