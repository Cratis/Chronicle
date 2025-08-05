// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Serialization;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.given;

public class a_constraint_builder_with_owner : Specification
{
    protected ConstraintBuilder _constraintBuilder;
    protected IEventTypes _eventTypes;
    protected Type _owner;
    protected NJsonSchemaGenerator _generator;

    void Establish()
    {
        _generator = new NJsonSchemaGenerator(new SystemTextJsonSchemaGeneratorSettings());

        _owner = typeof(Owner);
        _eventTypes = Substitute.For<IEventTypes>();
        _constraintBuilder = new ConstraintBuilder(_eventTypes, new DefaultNamingPolicy(), _owner);
    }

    record Owner();

    protected IConstraintDefinition CreateConstraint(ConstraintName name)
    {
        var constraint = Substitute.For<IConstraintDefinition>();
        constraint.Name.Returns(name);
        return constraint;
    }
}
