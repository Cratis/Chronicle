// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Setup.Execution;
using MongoDB.Driver;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_the_failure_is_not_ambiguous : Specification
{
    readonly Exception _ordinary = new InvalidOperationException("ordinary failure");
    readonly Exception _cancellation = new OperationCanceledException();
    Exception _ordinaryResult;
    Exception _cancellationResult;
    Exception _admission;
    Exception _singleAdmissionResult;
    Exception _batchAdmissionResult;

    async Task Because()
    {
        _ordinaryResult = await Catch.Exception(() => Task.FromException<AppendResult>(_ordinary).WithUnknownOutcome(Guid.NewGuid()));
        _cancellationResult = await Catch.Exception(() => Task.FromException<AppendManyResult>(_cancellation).WithUnknownOutcome(Guid.NewGuid()));

        var context = Substitute.For<IIncomingGrainCallContext>();
        context.Invoke().Returns(Task.FromException(new MongoWaitQueueFullException("pool is full")));
        _admission = await Catch.Exception(() => new SerializableFailureIncomingCallFilter().Invoke(context))
            ?? throw new InvalidOperationException("The call did not fail");
        _singleAdmissionResult = await Catch.Exception(() => Task.FromException<AppendResult>(_admission).WithUnknownOutcome(Guid.NewGuid()));
        _batchAdmissionResult = await Catch.Exception(() => Task.FromException<AppendManyResult>(_admission).WithUnknownOutcome(Guid.NewGuid()));
    }

    [Fact] void should_preserve_ordinary_failures() => _ordinaryResult.ShouldBeSame(_ordinary);
    [Fact] void should_preserve_cancellation() => _cancellationResult.ShouldBeSame(_cancellation);
    [Fact] void should_receive_the_existing_storage_wrapper() => _admission.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_receive_the_storage_category_from_the_filter() => _admission.Data["Cratis.Chronicle.StorageAdmission.Category"].ShouldEqual("MongoWaitQueueFull.v1");
    [Fact] void should_propagate_single_append_admission_instead_of_an_unknown_outcome() => _singleAdmissionResult.ShouldBeSame(_admission);
    [Fact] void should_propagate_batch_append_admission_instead_of_an_unknown_outcome() => _batchAdmissionResult.ShouldBeSame(_admission);
}
