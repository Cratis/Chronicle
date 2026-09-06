// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map;

/// <summary>
/// The subscription states the same upstream system uses now, on different underlying numbers.
/// </summary>
public enum SubscriptionState
{
    Unspecified = 100,
    Running = 101,
    Stopped = 102
}
