// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_metadata_at.and_the_identity_is_renamed.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_metadata_at;

/// <summary>
/// Verifies metadata observes a rename without rewriting the event.
/// </summary>
/// <param name="context">The integration context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_identity_is_renamed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public EventMetadata Before;
        public EventMetadata After;

        public override IEnumerable<Type> EventTypes => [typeof(MetadataRecorded)];

        async Task Because()
        {
            var subject = Guid.NewGuid().ToString();
            var provider = Services.GetRequiredService<IIdentityProvider>();
            provider.SetCurrentIdentity(new Identity(subject, "Original name", "username"));
            try
            {
                var appended = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new MetadataRecorded("payload"));
                Before = await EventStore.EventLog.GetMetadataAt(appended.SequenceNumber);
                await EventStore.Identities.Rename(subject, "Current name");
                After = await EventStore.EventLog.GetMetadataAt(appended.SequenceNumber);
            }
            finally
            {
                provider.ClearCurrentIdentity();
            }
        }
    }

    [Fact] void should_read_the_original_name_before_renaming() => Context.Before.CausedBy.Name.ShouldEqual("Original name");
    [Fact] void should_read_the_current_name_after_renaming() => Context.After.CausedBy.Name.ShouldEqual("Current name");
    [Fact] void should_preserve_the_event_locator() => Context.After.SequenceNumber.ShouldEqual(Context.Before.SequenceNumber);
}
