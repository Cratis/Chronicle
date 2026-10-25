// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_gated_state_write : an_observer_with_subscription_for_specific_event_type
{
    protected readonly write_gated_observer_state_storage _gatedStateStorage = new();
    protected readonly routing_reconciliation_logger _routingLogger = new();

    public an_observer_with_a_gated_state_write()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _gatedStateStorage : null!;
    }

    protected override ILogger CreateStateLogger(string category) =>
        string.Equals(category, typeof(Routing).FullName, StringComparison.Ordinal) ? _routingLogger : base.CreateStateLogger(category);
}
