// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies;

public class when_a_namespace_keyed_reply_is_selected_for_loss : given.a_namespace_append
{
    void Establish() => _replies.DropNext(DroppedReply.Append);

    async Task Because() => _error = await Catch.Exception(() => _subject.AppendOnceToNamespace(_request, _context));

    [Fact] void should_drop_only_after_the_real_call() => _inner.Received(1).AppendOnceToNamespace(_request, _context);
    [Fact] void should_report_the_deliberate_reply_loss() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_record_one_discarded_reply() => _replies.Dropped.ShouldEqual(1);
}
