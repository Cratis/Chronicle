// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_a_transient_transaction_error_aborts : Specification
{
    MongoException _error;
    Exception _classified;
    void Establish()
    {
        _error = new MongoException("Transaction aborted");
        _error.AddErrorLabel("TransientTransactionError");
    }
    void Because() => _classified = EventSequenceStorage.ClassifyAppendFailure(_error, true);
    [Fact] void should_report_not_committed() => _classified.ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_preserve_the_cause() => _classified.InnerException.ShouldBeSame(_error);
}
