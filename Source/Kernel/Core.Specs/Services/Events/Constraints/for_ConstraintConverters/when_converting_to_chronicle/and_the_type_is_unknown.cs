// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

public class and_the_type_is_unknown : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new Contracts.Events.Constraints.Constraint
    {
        Name = "unknown",
        Type = (Contracts.Events.Constraints.ConstraintType)999
    }.ToChronicle());

    [Fact] void should_fail_with_the_dedicated_error() => _error.ShouldBeOfExactType<UnknownConstraintType>();
}
