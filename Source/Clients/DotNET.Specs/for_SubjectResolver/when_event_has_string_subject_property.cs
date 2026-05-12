// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_SubjectResolver;

public class when_event_has_string_subject_property : Specification
{
    record EventWithStringSubject([property: Subject] string CustomerId, string Name);

    Subject? _result;

    void Because() => _result = SubjectResolver.ResolveFrom(new EventWithStringSubject("customer-99", "Alice"));

    [Fact] void should_resolve_subject() => _result.ShouldNotBeNull();
    [Fact] void should_carry_the_property_value() => _result!.Value.ShouldEqual("customer-99");
}
