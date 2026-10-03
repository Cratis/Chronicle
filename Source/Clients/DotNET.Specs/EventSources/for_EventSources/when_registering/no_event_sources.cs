// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources.when_registering;

public class no_event_sources : given.all_dependencies
{
    EventSources _subject;

    async Task Establish()
    {
        _subject = new EventSources(_eventStore, _clientArtifacts);
        await _subject.Discover();
    }

    async Task Because() => await _subject.Register();

    [Fact] void should_not_call_the_kernel() => _eventSourcesService.DidNotReceive().RegisterEventSources(Arg.Any<Contracts.EventSources.RegisterEventSourcesRequest>());
}
