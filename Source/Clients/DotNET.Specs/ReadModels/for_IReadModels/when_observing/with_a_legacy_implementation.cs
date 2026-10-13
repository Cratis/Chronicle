// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_IReadModels.when_observing;

public class with_a_legacy_implementation : Specification
{
    IReadModels _inner;
    IReadModels _readModels;
    Exception _failure;

    void Establish()
    {
        _inner = Substitute.For<IReadModels>();
        _readModels = new given.LegacyReadModels(_inner);
    }

    void Because() => _failure = Catch.Exception(() => _readModels.Observe<object>());

    [Fact] void should_refuse_collection_observation() => _failure.ShouldBeOfExactType<ReadModelCannotBeObserved>();
    [Fact] void should_name_the_read_model() => _failure.Message.ShouldContain(typeof(object).FullName!);
    [Fact] void should_not_open_a_stream_or_attempt_other_reads() => _inner.ReceivedCalls().ShouldBeEmpty();
}
