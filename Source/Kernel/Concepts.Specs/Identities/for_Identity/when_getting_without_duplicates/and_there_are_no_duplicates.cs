// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Identities.for_Identity.when_getting_without_duplicates;

public class and_there_are_no_duplicates : Specification
{
    Identity _root;
    Identity _result;

    void Establish() =>
        _root = new Identity("subject-a", "A", "a", new Identity("subject-b", "B", "b"));

    void Because() => _result = _root.WithoutDuplicates();

    [Fact] void should_keep_root_identity() => _result.Subject.ShouldEqual("subject-a");
    [Fact] void should_keep_on_behalf_of_identity() => _result.OnBehalfOf!.Subject.ShouldEqual("subject-b");
    [Fact] void should_have_no_further_chain() => _result.OnBehalfOf!.OnBehalfOf.ShouldBeNull();
}
