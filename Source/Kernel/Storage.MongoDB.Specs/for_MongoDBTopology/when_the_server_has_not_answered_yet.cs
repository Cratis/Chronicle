// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology;

public class when_the_server_has_not_answered_yet : Specification
{
    readonly TaskCompletionSource<BsonDocument?> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    MongoDBTopology _topology;
    bool _whileWaiting;
    bool _afterAnswer;

    async Task Because()
    {
        _topology = new(_ => _reply.Task, NullLogger.Instance, TimeProvider.System);
        _whileWaiting = _topology.IsStandalone;
        _reply.SetResult(new BsonDocument("isWritablePrimary", true));
        await _topology.Probing;
        _afterAnswer = _topology.IsStandalone;
    }

    [Fact] void should_answer_without_waiting_for_the_server() => _whileWaiting.ShouldBeFalse();
    [Fact] void should_classify_the_server_once_it_answers() => _afterAnswer.ShouldBeTrue();
}
