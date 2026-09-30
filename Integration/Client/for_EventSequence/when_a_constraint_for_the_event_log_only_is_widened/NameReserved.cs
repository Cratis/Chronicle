// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_a_constraint_for_the_event_log_only_is_widened;

[EventType]
public record NameReserved(string Name);
