// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_session_resumes_after_the_connection_was_lost : given.a_serve_service
{
    List<ServePullRequest> _first;
    List<ServePullRequest> _second;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because()
    {
        // The connection is lost after the first batch arrived. The next connection may land on another silo, which only has the inbox to go by.
        _first = await Dial(request => Answer(request, "customer-store", 0UL, 1UL, 2UL), frames => frames.Count >= 2);
        _inbox.Resume = new FederationResumePoint("hosted-acme", 2UL);
        _second = await Dial(
            request => Answer(request, "customer-store", request.FromSequenceNumber == 3UL ? [3UL, 4UL] : []),
            frames => frames.Count >= 3);
    }

    [Fact] void should_have_started_from_the_beginning_the_first_time() => _first[0].FromSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_resume_from_the_last_event_that_arrived() => _second[0].FromSequenceNumber.ShouldEqual(3UL);
    [Fact] void should_not_repeat_what_arrived_before() => _inbox.Appended.Select(_ => _.SequenceNumber).ToArray().ShouldEqual([0UL, 1UL, 2UL, 3UL, 4UL]);
}
