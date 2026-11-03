// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Setup.Execution;
using MongoDB.Driver;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_the_subscriber;

public class and_storage_admission_fails : Specification
{
    IObserverSubscriber _subscriber;
    Exception _admission;
    Exception _finiteWaitFailure;
    Exception _disabledWaitFailure;

    async Task Establish()
    {
        var context = Substitute.For<IIncomingGrainCallContext>();
        context.Invoke().Returns(Task.FromException(new MongoWaitQueueFullException("pool is full")));
        _admission = await Catch.Exception(() => new SerializableFailureIncomingCallFilter().Invoke(context))
            ?? throw new InvalidOperationException("The call did not fail");
        _subscriber = Substitute.For<IObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromException<ObserverSubscriberResult>(_admission));
    }

    async Task Because()
    {
        _finiteWaitFailure = await Catch.Exception(() => _subscriber.OnNextWithin(
            TimeSpan.FromMinutes(10), "some-partition", [], new ObserverSubscriberContext(null)));
        _disabledWaitFailure = await Catch.Exception(() => _subscriber.OnNextWithin(
            TimeSpan.Zero, "some-partition", [], new ObserverSubscriberContext(null)));
    }

    [Fact] void should_receive_the_existing_storage_wrapper() => _admission.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_receive_the_storage_category_from_the_filter() => _admission.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");
    [Fact] void should_propagate_admission_without_a_timeout_replacement_when_wait_is_finite() => _finiteWaitFailure.ShouldBeSame(_admission);
    [Fact] void should_propagate_admission_without_a_timeout_replacement_when_wait_is_disabled() => _disabledWaitFailure.ShouldBeSame(_admission);
    [Fact] void should_classify_the_finite_wait_failure_as_handling() => _finiteWaitFailure.ToFailureKind().ShouldEqual(FailureKind.Handling);
    [Fact] void should_classify_the_disabled_wait_failure_as_handling() => _disabledWaitFailure.ToFailureKind().ShouldEqual(FailureKind.Handling);
}
