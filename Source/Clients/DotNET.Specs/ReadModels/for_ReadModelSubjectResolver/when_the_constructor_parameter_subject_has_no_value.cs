// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_the_constructor_parameter_subject_has_no_value : Specification
{
    record ReadModelWithUnsetSubject(string Id, [Subject] Subject? UserId);

    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(new ReadModelWithUnsetSubject("signup-42", null));

    [Fact] void should_resolve_a_subject() => _result.ShouldNotBeNull();
    [Fact] void should_fall_back_to_the_id() => _result.Value.ShouldEqual("signup-42");
}
