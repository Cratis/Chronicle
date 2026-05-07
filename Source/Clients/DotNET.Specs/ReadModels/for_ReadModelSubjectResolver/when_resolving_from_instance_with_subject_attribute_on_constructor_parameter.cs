// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_resolving_from_instance_with_subject_attribute_on_constructor_parameter : Specification
{
    record ReadModelWithSubjectParam([Subject] string OwnerId, string Label);

    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(new ReadModelWithSubjectParam("owner-7", "Test"));

    [Fact] void should_resolve_a_subject() => _result.ShouldNotBeNull();
    [Fact] void should_carry_the_annotated_property_value() => _result!.Value.ShouldEqual("owner-7");
}
