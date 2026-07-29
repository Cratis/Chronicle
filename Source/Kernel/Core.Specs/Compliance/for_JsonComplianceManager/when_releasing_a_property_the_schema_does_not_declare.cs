// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// A stored document can carry a property its schema no longer declares — an event type that gained or renamed a
/// property without a migration. The mismatch used to surface as a bare LINQ error naming nothing at all, so the
/// diagnostic has to name the property, the subject, and what the schema does declare.
/// </summary>
public class when_releasing_a_property_the_schema_does_not_declare : given.a_value_handler_and_a_type_with_one_property
{
    const string Identifier = "request-42";
    const string UnknownPropertyName = "propertyTheSchemaNeverHeardOf";

    Exception _exception;

    void Establish() => _input[UnknownPropertyName] = "some value";

    async Task Because() => _exception = await Catch.Exception(() => _manager.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        Identifier,
        _input));

    [Fact] void should_fail_with_the_property_not_found_exception() => _exception.ShouldBeOfExactType<CompliancePropertyNotFoundInSchema>();

    [Fact] void should_name_the_property_that_is_missing() => _exception.Message.ShouldContain(UnknownPropertyName);

    [Fact] void should_name_the_subject_it_was_released_under() => _exception.Message.ShouldContain(Identifier);

    [Fact] void should_name_the_action() => _exception.Message.ShouldContain(ComplianceMetadataActionFailed.ReleaseAction);

    [Fact] void should_list_the_properties_the_schema_declares() => _exception.Message.ShouldContain(PropertyName);
}
