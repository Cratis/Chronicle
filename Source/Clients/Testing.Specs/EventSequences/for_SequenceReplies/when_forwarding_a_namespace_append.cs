// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies;

public class when_forwarding_a_namespace_append : given.a_namespace_append
{
    async Task Because() => _actual = await _subject.AppendOnceToNamespace(_request, _context);

    [Fact] void should_forward_the_original_request_and_context() => _inner.Received(1).AppendOnceToNamespace(_request, _context);
    [Fact] void should_preserve_the_captured_identity() => _request.ExpectedIncarnation.ShouldEqual(_identity);
    [Fact] void should_return_the_original_envelope() => _actual.ShouldBeSame(_response);
    [Fact] void should_not_drop_an_unselected_reply() => _replies.Dropped.ShouldEqual(0);
}
