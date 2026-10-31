// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_a_user_supplies_another_identity : given.a_caller
{
    void Establish() => _caller = ChronicleUser();
    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_use_the_caller_subject() => _result.Subject.ShouldEqual("caller");
    [Fact] void should_use_the_caller_name() => _result.Name.ShouldEqual("Caller");
    [Fact] void should_use_the_caller_username() => _result.UserName.ShouldEqual("caller-name");
    [Fact] void should_not_retain_a_supplied_chain() => _result.OnBehalfOf.ShouldBeNull();
}
