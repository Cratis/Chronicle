// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content.and_pii_is_erased.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content;

[Collection(ChronicleCollection.Name)]
public class and_pii_is_erased(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ContactRecorded)];
        public ContentVerificationResult Result { get; private set; }

        public async Task Because()
        {
            var source = EventSourceId.New();
            var append = await EventStore.EventLog.Append(source, new ContactRecorded("private", "reference"));
            append.IsSuccess.ShouldBeTrue();
            await EventStore.PII.DeleteEncryptionKeyFor(source.Value);
            var prepared = await EventStore.EventLog.Prepare(new ContactRecorded(string.Empty, "reference"));
            Result = await EventStore.EventLog.VerifyContent(append.SequenceNumber, prepared, source);
        }
    }

    [Fact] void should_never_mistake_erasure_for_an_empty_value() => Context.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
