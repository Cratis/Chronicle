// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_first_registrations_conflict : given.concurrent_first_registrations
{
    void Establish() => _secondSchema = """{"type":"object","properties":{"value":{"type":"integer"}}}""";

    Task Because() => RegisterConcurrently();

    [Fact] void should_reject_exactly_one_caller() => _errors.Length.ShouldEqual(1);
    [Fact] void should_report_the_existing_schema_conflict() => _errors.Single().ShouldBeOfExactType<EventTypeSchemaChanged>();
    [Fact] void should_store_exactly_one_generation() => _stored.Generations.Count().ShouldEqual(1);
    [Fact] void should_record_only_one_new_event_type() => _sequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Append").ShouldEqual(1);
    [Fact] void should_preserve_the_successful_callers_schema() => _stored.Generations.Single().Schema.Properties["value"].Type.ShouldEqual(_successfulSchemaType);
}
