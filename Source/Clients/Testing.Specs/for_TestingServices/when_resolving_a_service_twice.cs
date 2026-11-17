// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_resolving_a_service_twice : given.testing_services
{
    [Fact] void should_return_the_same_observers() => _services.Observers.ShouldBeSame(_services.Observers);
    [Fact] void should_return_the_same_reactors() => _services.Reactors.ShouldBeSame(_services.Reactors);
    [Fact] void should_return_the_same_sequences() => _services.Sequences.ShouldBeSame(_services.Sequences);
    [Fact] void should_return_the_same_occurrences() => _services.Occurrences.ShouldBeSame(_services.Occurrences);
    [Fact] void should_return_the_same_read_models() => _services.ReadModels.ShouldBeSame(_services.ReadModels);
    [Fact] void should_return_the_same_compliance() => _services.Compliance.ShouldBeSame(_services.Compliance);
    [Fact] void should_return_the_same_event_types() => _services.EventTypes.ShouldBeSame(_services.EventTypes);
}
