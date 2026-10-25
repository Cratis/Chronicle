// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Receivers;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_a_delivery;

public class for_an_unknown_reactor : given.all_dependencies
{
    Exception _error;
    async Task Because() => _error = await Catch.Exception(() => ((IReceiveReactorDeliveries)_reactors).Deliver("missing", new EventsToObserve(), CancellationToken.None));
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<UnknownReactorId>();
}
