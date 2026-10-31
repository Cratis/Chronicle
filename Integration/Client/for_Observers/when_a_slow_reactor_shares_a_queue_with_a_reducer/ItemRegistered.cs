// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer;

[EventType]
public record ItemRegistered(string Name);
