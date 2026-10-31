// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_creating;

public class with_a_short_key : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _ = new IdentifierHasher(new string('x', 31)));

    [Fact] void should_reject_the_key() => _exception.ShouldBeOfExactType<InvalidIdentifierHashKey>();
    [Fact] void should_not_expose_the_key() => _exception.Message.ShouldEqual("Identifier hash key must be at least 32 characters");
}
