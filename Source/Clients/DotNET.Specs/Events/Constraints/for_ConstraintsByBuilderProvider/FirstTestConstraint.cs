// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintsByBuilderProvider;

public class FirstTestConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        var first = Substitute.For<IConstraintDefinition>();
        first.Name.Returns((ConstraintName)"FirstConstraintFirstDefinition");
        var second = Substitute.For<IConstraintDefinition>();
        second.Name.Returns((ConstraintName)"FirstConstraintSecondDefinition");
        builder.AddConstraint(first);
        builder.AddConstraint(second);
    }
}
