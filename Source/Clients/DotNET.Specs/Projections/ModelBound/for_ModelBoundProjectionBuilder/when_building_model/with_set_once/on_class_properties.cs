// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_once;

public class on_class_properties : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;
    FromDefinition _fromOpened;

    void Because()
    {
        _result = builder.Build(typeof(Account));
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _fromOpened = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
    }

    [Fact] void should_wrap_the_event_property_in_set_once() => _fromOpened.Properties[nameof(Account.OriginalName)].ShouldEqual("$setOnce(Name)");
    [Fact] void should_wrap_the_event_context_property_in_set_once() => _fromOpened.Properties[nameof(Account.Opened)].ShouldEqual("$setOnce($eventContext(Occurred))");

    class Account
    {
        [Key]
        public Guid Id { get; set; }

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name), SetOnce = true)]
        public string OriginalName { get; set; } = string.Empty;

        [SetFromContext<DebitAccountOpened>(nameof(EventContext.Occurred), SetOnce = true)]
        public DateTimeOffset Opened { get; set; }
    }
}
