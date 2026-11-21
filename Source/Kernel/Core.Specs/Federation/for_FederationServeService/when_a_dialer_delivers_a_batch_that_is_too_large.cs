// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_delivers_a_batch_that_is_too_large : given.a_serve_service
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
            // Every event is within the limit of one event, together they are not within the limit of a batch.
            var limits = _options.Federation.Limits;
            var content = $"{{\"value\":\"{new string('x', limits.MaxEventContentBytes - 100)}\"}}";
            var count = (int)(limits.MaxBatchBytes / (limits.MaxEventContentBytes - 100)) + 1;
            var answer = Answer(request, "customer-store");
            answer.Batch.Events = [.. Enumerable.Range(0, count).Select(_ =>
            {
                var item = Event((ulong)_, "type-a");
                item.Content = content;
                return item;
            })];
            return answer;
        },
        frames => frames.Count >= 3);

    [Fact] void should_append_nothing_of_the_batch() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_deny_the_dialer() => _frames[^1].Status.ShouldEqual(FederatedEventBatchStatus.Denied);
}
