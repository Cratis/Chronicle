// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.given;

public class no_constraints : Specification
{
    protected EventStoreName _eventStoreName = "SomeEventStore";

    protected IEventStore _eventStore;
    protected IInstancesOf<ICanProvideConstraints> _constraintsProviders;
    protected ICanProvideConstraints _constraintsProvider;
    protected Constraints _constraints;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(_eventStoreName);
        _constraintsProvider = Substitute.For<ICanProvideConstraints>();
        _constraintsProviders = new KnownInstancesOf<ICanProvideConstraints>([_constraintsProvider]);
        _constraints = new Constraints(_eventStore, _constraintsProviders);
    }
}


public class two_constraints : no_constraints
{
    protected static readonly ConstraintName _firstConstraintName = "FirstConstraint";
    protected static readonly ConstraintName _secondConstraintName = "SecondConstraint";
    protected IConstraintDefinition _firstConstraint;
    protected IConstraintDefinition _secondConstraint;

    void Establish()
    {
        _firstConstraint = Substitute.For<IConstraintDefinition>();
        _firstConstraint.Name.Returns(_firstConstraintName);
        _secondConstraint = Substitute.For<IConstraintDefinition>();
        _secondConstraint.Name.Returns(_secondConstraintName);

        _constraintsProvider.Provide().Returns(new[] { _firstConstraint, _secondConstraint }.ToImmutableList());
    }
}
