// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

public class when_has_constraint_violations : Specification
{
    AppendManyResult _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation("SomeEvent", 42UL, "constraint", "message", []);
        _result = new AppendManyResult
        {
            ConstraintViolations = [_violation]
        };
    }

    [Fact] void should_have_constraint_violations() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_the_violation() => _result.ConstraintViolations.ShouldContain(_violation);
}
