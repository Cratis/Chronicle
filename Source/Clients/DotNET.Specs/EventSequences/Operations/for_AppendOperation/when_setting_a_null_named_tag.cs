// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Operations.for_AppendOperation;

public class when_setting_a_null_named_tag : Specification
{
    Exception _error;
    AppendOperation _operation;

    void Because() => _error = Catch.Exception(() => _operation = new AppendOperation("event") { NamedTags = [null!] });

    [Fact] void should_reject_the_invalid_tag_by_name() => _error.ShouldBeOfExactType<InvalidNamedTag>();
}
