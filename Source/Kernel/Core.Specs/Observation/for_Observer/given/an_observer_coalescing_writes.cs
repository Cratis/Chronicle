// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_coalescing_writes : an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 600 };
}
