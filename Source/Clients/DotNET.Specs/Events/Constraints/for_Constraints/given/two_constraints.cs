// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.given;

public class two_constraints : no_constraints
{
    protected static readonly ConstraintName _firstConstraintName = "FirstConstraint";
    protected static readonly ConstraintName _secondConstraintName = "SecondConstraint";
    protected const string _firstEventTypeName = "FirstEventType";
    protected const string _secondEventTypeName = "SecondEventType";
    protected const string _firstProperty = "firstProperty";
    protected const string _secondProperty = "secondProperty";

    protected IConstraintDefinition _firstConstraint;
    protected IConstraintDefinition _secondConstraint;

    async Task Establish()
    {
        _firstConstraint = new UniqueConstraintDefinition(_firstConstraintName,
            _ => (ConstraintViolationMessage)$"First {{{_firstProperty}}} second {{{_secondProperty}}}",
            [new UniqueConstraintEventDefinition(_firstEventTypeName, [_firstProperty])],
            null,
            false);

        _secondConstraint = new UniqueConstraintDefinition(_secondConstraintName,
            _ => (ConstraintViolationMessage)$"First {{{_firstProperty}}} second {{{_secondProperty}}}",
            [new UniqueConstraintEventDefinition(_secondEventTypeName, [_secondProperty])],
            null,
            false);

        _constraintsProvider.Provide().Returns(new[] { _firstConstraint, _secondConstraint }.ToImmutableList());
        await _constraints.Discover();
    }
}
