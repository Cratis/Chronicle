// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_a_user_supplies_a_long_chain : given.a_caller
{
    void Establish()
    {
        _caller = ChronicleUser();
        for (var index = 0; index < 256; index++)
        {
            _supplied = new("other", "Other", "other-name", _supplied);
        }
    }

    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_use_the_caller() => _result.Subject.ShouldEqual("caller");
    [Fact] void should_discard_the_entire_chain() => _result.OnBehalfOf.ShouldBeNull();
}
