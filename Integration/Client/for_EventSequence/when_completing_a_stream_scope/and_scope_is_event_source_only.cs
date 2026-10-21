// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope.and_scope_is_event_source_only.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope;

[Collection(ChronicleCollection.Name)]
public class and_scope_is_event_source_only(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public bool Completed;
        public IAppendResult DefaultStream;
        public IAppendResult AnotherSource;

        async Task Because()
        {
            Completed = (await EventStore.EventLog.CompleteStream(ClosedStreamScope.ForEventSource(Source))).IsSuccess;
            DefaultStream = await EventStore.EventLog.Append(Source, new given.StreamActivity("default"));
            AnotherSource = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new given.StreamActivity("other"));
        }
    }

    [Fact] void should_complete_the_scope() => Context.Completed.ShouldBeTrue();
    [Fact] void should_reject_the_sources_default_stream() => Context.DefaultStream.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_allow_another_sources_default_stream() => Context.AnotherSource.IsSuccess.ShouldBeTrue();
}
