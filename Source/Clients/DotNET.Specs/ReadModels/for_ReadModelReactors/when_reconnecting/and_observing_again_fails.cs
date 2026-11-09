// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_observing_again_fails : given.materialized_reactors_on_a_lifecycle
{
    Exception? _error;

    void Because()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _observeFailure = new UnknownReadModel(typeof(Item));
        _error = Catch.Exception(_reactors.Start);
        _observeFailure = null;
        _reactors.Start();
    }

    [Fact] void should_not_fail_the_registration() => _error.ShouldBeNull();
    [Fact] void should_observe_instances_on_the_next_registration() => _streams.Count.ShouldEqual(2);
}
