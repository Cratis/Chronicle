// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content.and_content_differs.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content;

[Collection(ChronicleCollection.Name)]
public class and_content_differs(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ContactRecorded)];
        public ContentVerificationResult Result { get; private set; }
        public ContentVerificationResult Expected => ExpectedVerification.For(ChronicleFixture, ContentVerificationResult.Different);

        public async Task Because()
        {
            var source = EventSourceId.New();
            var append = await EventStore.EventLog.Append(source, new ContactRecorded("private", "reference"));
            append.IsSuccess.ShouldBeTrue();
            var prepared = await EventStore.EventLog.Prepare(new ContactRecorded("different", "reference"));
            Result = await EventStore.EventLog.VerifyContent(append.SequenceNumber, prepared, source);
        }
    }

    [Fact] void should_report_different_where_the_backend_can_compare() => Context.Result.ShouldEqual(Context.Expected);
}
