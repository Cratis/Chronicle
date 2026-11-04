// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageErrorClassifier.when_classifying;

public class and_the_wait_queue_failure_is_nested : Specification
{
    bool _result;

    void Because() => _result = new MongoDBStorageErrorClassifier().IsTransient(new InvalidOperationException("outer", new MongoWaitQueueFullException("pool is full")));
    [Fact] void should_be_transient() => _result.ShouldBeTrue();
}
