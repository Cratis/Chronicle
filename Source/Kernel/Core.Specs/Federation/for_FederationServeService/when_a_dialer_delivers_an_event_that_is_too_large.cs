// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_delivers_an_event_that_is_too_large : given.a_serve_service
{
    List<ServePullRequest> _frames;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because() => _frames = await Dial(
        request =>
        {
            var answer = Answer(request, "customer-store", 0UL, 1UL);
            var large = Event(2UL, "type-a");
            large.Content = $"{{\"value\":\"{new string('x', _options.Federation.Limits.MaxEventContentBytes)}\"}}";
            answer.Batch.Events = [.. answer.Batch.Events, large];
            return answer;
        },
        frames => frames.Count >= 3);

    [Fact] void should_append_nothing_of_the_batch() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_deny_the_dialer() => _frames[^1].Status.ShouldEqual(FederatedEventBatchStatus.Denied);
}
