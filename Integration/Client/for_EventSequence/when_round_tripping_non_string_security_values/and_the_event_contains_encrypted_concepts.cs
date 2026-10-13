// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_round_tripping_non_string_security_values.and_the_event_contains_encrypted_concepts.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_round_tripping_non_string_security_values;

[Collection(ChronicleCollection.Name)]
public class and_the_event_contains_encrypted_concepts(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public SecretValuesRecorded Read;
        public bool AppendSucceeded;

        public override IEnumerable<Type> EventTypes => [typeof(SecretValuesRecorded)];

        async Task Because()
        {
            var source = Guid.NewGuid().ToString();
            var result = await EventStore.EventLog.Append(source, new SecretValuesRecorded(new SecretCount(37), new SecretFlag(true)));
            AppendSucceeded = result.IsSuccess;
            var events = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(source, [typeof(SecretValuesRecorded).GetEventType()]);
            Read = (SecretValuesRecorded)events.Single().Content;
        }
    }

    [Fact] void should_append_successfully() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_release_the_integer_concept() => Context.Read.Count.Value.ShouldEqual(37);
    [Fact] void should_release_the_boolean_concept() => Context.Read.Flag.Value.ShouldEqual(true);
}
