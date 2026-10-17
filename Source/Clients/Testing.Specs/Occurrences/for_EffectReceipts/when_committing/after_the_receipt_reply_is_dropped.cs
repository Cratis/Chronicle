// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_committing;

public class after_the_receipt_reply_is_dropped : given.a_store
{
    Exception? _error;
    EffectReceiptResult _retry;

    async Task Because()
    {
        _store.Replies.DropNext(DroppedReply.RecordEffectReceipt);
        _error = await Catch.Exception(() => _store.Occurrences.RecordEffectReceipt(_key, new NoEvents(), JsonNode.Parse("{\"result\":1}")));
        _retry = await _store.Occurrences.RecordEffectReceipt(_key, new NoEvents(), JsonNode.Parse("{\"result\":2}"));
    }

    [Fact] void should_discard_the_committed_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_deduplicate_the_retry() => _retry.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
    [Fact] void should_return_the_winning_response() => _retry.Receipt.Response!["result"]!.GetValue<int>().ShouldEqual(1);
}
