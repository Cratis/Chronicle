// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_creating;

public class with_a_null_key : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _ = new IdentifierHasher(null!));

    [Fact] void should_reject_the_key() => _exception.ShouldBeOfExactType<InvalidIdentifierHashKey>();
}
