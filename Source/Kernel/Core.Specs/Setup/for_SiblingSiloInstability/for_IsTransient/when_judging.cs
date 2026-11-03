// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Execution;

namespace Cratis.Chronicle.Core.Specs.Setup.for_SiblingSiloInstability.for_IsTransient;

public class when_judging : Specification
{
    const string WaitQueueFullMessage = "MongoDB.Driver.MongoWaitQueueFullException: The wait queue for acquiring a connection to server is full.";

    Exception _carriedWaitQueueFull;
    Exception _otherCarriedMongoFailure;
    Exception _unrelatedInvalidOperation;
    Exception _timeout;

    void Establish()
    {
        _carriedWaitQueueFull = new InvalidOperationException(WaitQueueFullMessage);
        _otherCarriedMongoFailure = new InvalidOperationException("MongoDB.Driver.MongoAuthenticationException: Unable to authenticate.");
        _unrelatedInvalidOperation = new InvalidOperationException("Sequence contains no elements");
        _timeout = new TimeoutException("Response did not arrive on time");
    }

    [Fact] void should_retry_a_wait_queue_failure_that_crossed_a_silo_boundary() => SiblingSiloInstability.IsTransient(_carriedWaitQueueFull).ShouldBeTrue();
    [Fact] void should_retry_a_wait_queue_failure_produced_by_the_serializable_failure_filter() => SiblingSiloInstability.IsTransient(SerializableFailureIncomingCallFilter.Replace(new MongoDB.Driver.MongoWaitQueueFullException("The wait queue is full."))).ShouldBeTrue();
    [Fact] void should_retry_a_timeout() => SiblingSiloInstability.IsTransient(_timeout).ShouldBeTrue();
    [Fact] void should_not_retry_another_carried_storage_failure() => SiblingSiloInstability.IsTransient(_otherCarriedMongoFailure).ShouldBeFalse();
    [Fact] void should_not_retry_an_unrelated_invalid_operation() => SiblingSiloInstability.IsTransient(_unrelatedInvalidOperation).ShouldBeFalse();
}
