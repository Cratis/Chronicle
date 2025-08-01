// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.given;

public class a_unique_constraint_builder_without_owner : all_dependencies
{
    protected UniqueConstraintBuilder _constraintBuilder;
    void Establish()
    {
        _constraintBuilder = new UniqueConstraintBuilder(_eventTypes, _namingPolicy);
    }
}
