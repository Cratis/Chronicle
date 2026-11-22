// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_first_registrations_are_identical : given.concurrent_first_registrations
{
    void Establish() => _secondSchema = FirstSchema;

    Task Because() => RegisterConcurrently();

    [Fact] void should_accept_both_callers() => _errors.ShouldBeEmpty();
    [Fact] void should_store_exactly_one_generation() => _stored.Generations.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_registered_schema() => _stored.Generations.Single().Schema.Properties["value"].Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_record_only_one_new_event_type() => _sequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Append").ShouldEqual(1);
}
