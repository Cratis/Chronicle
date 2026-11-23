// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_burst_of_reactors;

[EventType]
public record BurstEvent(int Number);
