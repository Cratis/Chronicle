// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.an_event_with_nullable_properties_set_to_null.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class an_event_with_nullable_properties_set_to_null(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EventWithNullableProperties)];

        public IAppendResult Result { get; private set; }

        async Task Because() =>
            Result = await EventStore.EventLog.Append("source", new EventWithNullableProperties("Test", null, null));
    }

    [Fact] void should_succeed() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_constraint_violations() => Context.Result.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_not_have_errors() => Context.Result.HasErrors.ShouldBeFalse();
}
