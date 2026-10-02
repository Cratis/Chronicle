// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources.when_discovering;

public class an_event_source_with_the_same_stream_twice : given.all_dependencies
{
    EventSources _subject;
    Exception _exception;

    void Establish()
    {
        _clientArtifacts.EventSources.Returns([typeof(TwiceEventSource)]);
        _subject = new EventSources(_eventStore, _clientArtifacts);
    }

    async Task Because() => _exception = await Catch.Exception(_subject.Discover);

    [Fact] void should_fail_discovery() => _exception.ShouldBeOfExactType<DuplicateEventStreamName>();
}
