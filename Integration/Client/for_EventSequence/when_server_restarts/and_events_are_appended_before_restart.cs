// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_server_restarts.and_events_are_appended_before_restart.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_server_restarts;

[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_before_restart(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "reconnect-test-source";
        public EventSequenceNumber SequenceNumberAfterRestart { get; private set; }
        public IAppendResult AppendResultBeforeRestart { get; private set; }
        public IAppendResult AppendResultAfterRestart { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Establish()
        {
            AppendResultBeforeRestart = await EventStore.EventLog.Append(EventSourceId, new SomeEvent("before-restart"));
        }

        async Task Because()
        {
            // Use the fixture's RestartStorageAsync to handle all storage backends.
            // For MongoDB: restarts the MongoDB process or container.
            // For SQL: restarts the database container so the server must reconnect.
            // For SQLite: restarts the server container (data persists via overlay filesystem).
            await ChronicleFixture.RestartStorageAsync();

            // Wait until the kernel has reconnected to the storage backend after the restart.
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    await EventStore.EventLog.GetNextSequenceNumber();
                    break;
                }
                catch
                {
                    await Task.Delay(500);
                }
            }

            AppendResultAfterRestart = await EventStore.EventLog.Append(EventSourceId, new SomeEvent("after-restart"));
            SequenceNumberAfterRestart = await EventStore.EventLog.GetNextSequenceNumber();
        }
    }

    [Fact] void should_successfully_append_event_before_restart() => Context.AppendResultBeforeRestart.IsSuccess.ShouldBeTrue();
    [Fact] void should_successfully_append_event_after_restart() => Context.AppendResultAfterRestart.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_two_events_in_the_log() => Context.SequenceNumberAfterRestart.Value.ShouldEqual(2ul);
}
