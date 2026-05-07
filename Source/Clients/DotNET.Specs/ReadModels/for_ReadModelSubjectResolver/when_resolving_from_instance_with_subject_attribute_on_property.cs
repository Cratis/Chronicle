// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_resolving_from_instance_with_subject_attribute_on_property : Specification
{
    class ReadModelWithExplicitSubject
    {
        [Subject]
        public string CustomerId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(new ReadModelWithExplicitSubject { CustomerId = "cust-42", Name = "Alice" });

    [Fact] void should_resolve_a_subject() => _result.ShouldNotBeNull();
    [Fact] void should_carry_the_annotated_property_value() => _result!.Value.ShouldEqual("cust-42");
}
