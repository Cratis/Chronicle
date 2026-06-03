// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream.and_stream_is_default.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream;

[Collection(ChronicleCollection.Name)]
public class and_stream_is_default(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public bool CompletedSuccessfully { get; private set; } = true;
        public string FailureName { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(SomeStreamEvent)];

        async Task Because()
        {
            var result = await EventStore.EventLog.CompleteStream(EventStreamType.All, EventStreamId.Default);
            CompletedSuccessfully = result.IsSuccess;
            if (result.TryGetError(out var error))
            {
                FailureName = error.ToString();
            }
        }
    }

    [Fact] void should_not_be_successful() => Context.CompletedSuccessfully.ShouldBeFalse();

    [Fact] void should_report_default_stream_cannot_be_completed() => Context.FailureName.ShouldEqual("DefaultStreamCannotBeCompleted");
}
