// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor;

public class when_registering : Specification
{
    ReactorAttribute _attribute;

    void Because() => _attribute = typeof(AlertIncidentsReactor).GetCustomAttributes(typeof(ReactorAttribute), false).Cast<ReactorAttribute>().Single();

    [Fact] void should_register_the_exact_id() => _attribute.Id.Value.ShouldEqual(AlertIncidentsReactor.Id);
    [Fact] void should_use_the_system_sequence() => _attribute.EventSequenceId.Value.ShouldEqual(WellKnownEventSequences.System);
    [Fact] void should_restrict_to_the_system_store() => _attribute.IsSystemEventStoreOnly.ShouldBeTrue();
    [Fact] void should_restrict_to_default_namespace() => _attribute.DefaultNamespaceOnly.ShouldBeTrue();
    [Fact] void should_be_excluded_from_observer_alerting() => AlertIncidentsReactor.ObserverKey.ObserverId.Value.StartsWith(AlertObservers.Prefix, StringComparison.Ordinal).ShouldBeTrue();
}
