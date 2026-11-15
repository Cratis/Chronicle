// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverBatchSize;

public class when_an_observer_declares_a_negative_bound : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ObserverBatchSize.For(typeof(Observer)));

    [Fact] void should_throw_invalid_batch_size() => _error.ShouldBeOfExactType<InvalidBatchSize>();
    [Fact] void should_name_the_property() => _error.Message.ShouldContain(nameof(BatchSizeAttribute.MaxDelayMilliseconds));

    [BatchSize(MaxDelayMilliseconds = -1)]
    public class Observer;
}
