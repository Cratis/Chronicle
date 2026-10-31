// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_the_commit_result_is_unknown : Specification
{
    MongoException _error;
    Exception _classified;
    void Establish()
    {
        _error = new MongoException("Commit reply lost");
        _error.AddErrorLabel("UnknownTransactionCommitResult");
        _error.AddErrorLabel("TransientTransactionError");
    }
    void Because() => _classified = EventSequenceStorage.ClassifyAppendFailure(_error, true);
    [Fact] void should_preserve_the_unknown_outcome_even_with_a_transient_label() => _classified.ShouldBeSame(_error);
}
