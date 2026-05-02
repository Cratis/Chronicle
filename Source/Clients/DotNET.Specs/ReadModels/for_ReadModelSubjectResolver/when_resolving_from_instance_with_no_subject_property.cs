// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelSubjectResolver;

public class when_resolving_from_instance_with_no_subject_property : Specification
{
    class ReadModelWithNoSubject
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    Subject? _result;

    void Because() => _result = ReadModelSubjectResolver.ResolveFrom(new ReadModelWithNoSubject { Name = "Alice", Count = 5 });

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
