// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content.and_content_matches.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content;

[Collection(ChronicleCollection.Name)]
public class and_content_matches(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ContactRecorded)];
        public ContentVerificationResult Result { get; private set; }
        public ContentVerificationResult Expected => ExpectedVerification.For(ChronicleFixture, ContentVerificationResult.Equal);

        public async Task Because()
        {
            var source = EventSourceId.New();
            var prepared = await EventStore.EventLog.Prepare(new ContactRecorded("private", "reference"));
            var append = await EventStore.EventLog.AppendPrepared(source, prepared);
            append.IsSuccess.ShouldBeTrue();
            Result = await EventStore.EventLog.VerifyContent(append.SequenceNumber, prepared, source);
        }
    }

    [Fact] void should_verify_the_complete_released_content_where_the_backend_can_compare() => Context.Result.ShouldEqual(Context.Expected);
}
