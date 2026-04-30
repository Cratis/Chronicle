// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_SubjectResolver;

public class when_event_has_null_subject_property : Specification
{
    record EventWithNullableSubject([property: Subject] string? CustomerId, string Name);

    Subject? _result;

    void Because() => _result = SubjectResolver.ResolveFrom(new EventWithNullableSubject(null, "Alice"));

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
