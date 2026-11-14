// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_delivers_more_events_than_were_asked_for : given.a_serve_service
{
    List<ServePullRequest> _frames;
    int _limit;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
        _limit = _options.Federation.Limits.MaxBatchSize;
    }

    async Task Because() => _frames = await Dial(
        request => Answer(request, "customer-store", [.. Enumerable.Range(0, _limit + 1).Select(_ => (ulong)_)]),
        frames => frames.Count >= 3);

    [Fact] void should_ask_for_the_limit() => _frames[0].Limit.ShouldEqual(_limit);
    [Fact] void should_append_nothing_of_the_oversized_batch() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_deny_the_dialer() => _frames[^1].Status.ShouldEqual(FederatedEventBatchStatus.Denied);
}
