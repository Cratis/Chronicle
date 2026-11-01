// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_there_is_no_principal_or_supplied_identity : given.a_caller
{
    void Establish() => _supplied = null;
    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_keep_the_anonymous_identity() => _result.Subject.ShouldEqual("anonymous");
}
