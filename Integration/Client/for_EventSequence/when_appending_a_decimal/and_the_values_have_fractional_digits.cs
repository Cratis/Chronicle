// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_a_decimal.and_the_values_have_fractional_digits.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_a_decimal;

[Collection(ChronicleCollection.Name)]
public class and_the_values_have_fractional_digits(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId SourceId { get; } = "decimal-source";
        public Amounts? ReadModel { get; private set; }
        public override IEnumerable<Type> EventTypes => [typeof(AmountRecorded)];
        public override IEnumerable<Type> Projections => [typeof(AmountsProjection)];

        async Task Because()
        {
            var handler = EventStore.Projections.GetHandlerFor<AmountsProjection>();
            await handler.WaitTillSubscribed();
            var result = await EventStore.EventLog.Append(SourceId, new AmountRecorded(193.58m, 1234567890.123456789012345678m));
            result.IsSuccess.ShouldBeTrue();
            await handler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
            ReadModel = await EventStore.ReadModels.GetInstanceById<Amounts>(SourceId.Value);
        }
    }

    [Fact] Task should_read_the_exact_amount() => Context.ShouldHaveAppendedEvent<AmountRecorded>(0, Context.SourceId.Value, value => decimal.GetBits(value.Amount).ShouldEqual(decimal.GetBits(193.58m)));
    [Fact] Task should_read_all_significant_digits() => Context.ShouldHaveAppendedEvent<AmountRecorded>(0, Context.SourceId.Value, value => decimal.GetBits(value.Precise).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m)));
    [Fact] void should_materialize_the_read_model() => Context.ReadModel.ShouldNotBeNull();
    [Fact] void should_project_the_exact_amount() => decimal.GetBits(Context.ReadModel!.Amount).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_project_all_significant_digits() => decimal.GetBits(Context.ReadModel!.Precise).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
