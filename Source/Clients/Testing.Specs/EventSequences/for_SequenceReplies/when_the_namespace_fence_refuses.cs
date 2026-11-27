// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies;

public class when_the_namespace_fence_refuses : given.a_namespace_append
{
    void Establish()
    {
        _replies.DropNext(DroppedReply.Append);
        _response.ExceptionMessages.Add(NamespaceErrorCodes.IncarnationMismatch);
    }

    async Task Because() => _actual = await _subject.AppendOnceToNamespace(_request, _context);

    [Fact] void should_preserve_the_refusal() => _actual.ExceptionMessages.ShouldEqual<string>([NamespaceErrorCodes.IncarnationMismatch]);
    [Fact] void should_not_fabricate_success() => _actual.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_drop_a_refused_call() => _replies.Dropped.ShouldEqual(0);
    [Fact] void should_keep_the_original_envelope() => _actual.ShouldBeSame(_response);
}
