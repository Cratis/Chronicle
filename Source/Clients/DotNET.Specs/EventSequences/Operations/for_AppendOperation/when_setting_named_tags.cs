// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Operations.for_AppendOperation;

public class when_setting_named_tags : Specification
{
    AppendOperation _operation;
    List<NamedTag> _input;

    void Establish()
    {
        _input = [new("key", "value")];
        _operation = new("event") { NamedTags = _input };
    }

    void Because() => _input.Add(new NamedTag("another", "value"));

    [Fact] void should_snapshot_the_input() => _operation.NamedTags.Select(_ => _.Name.Value).ShouldEqual(["key"]);
}
