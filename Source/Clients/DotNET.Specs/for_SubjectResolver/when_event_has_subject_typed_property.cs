// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_SubjectResolver;

public class when_event_has_subject_typed_property : Specification
{
    record EventWithSubjectProperty([property: Subject] Subject Customer, string Name);

    Subject? _result;

    void Because() => _result = SubjectResolver.ResolveFrom(new EventWithSubjectProperty(new Subject("subject-42"), "Alice"));

    [Fact] void should_resolve_subject() => _result.ShouldNotBeNull();
    [Fact] void should_carry_the_property_value() => _result!.Value.ShouldEqual("subject-42");
}
