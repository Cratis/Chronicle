// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintsByBuilderProvider;

public class when_providing_two_builders : Specification
{
    IConstraint _firstConstraint;
    IConstraint _secondConstraint;
    ConstraintsByBuilderProvider _provider;
    IClientArtifactsProvider _clientArtifactsProvider;
    IServiceProvider _serviceProvider;
    IEventTypes _eventTypes;
    IConstraintDefinition _firstConstraintFirstDefinition;
    IConstraintDefinition _firstConstraintSecondDefinition;
    IConstraintDefinition _secondConstraintFirstDefinition;
    IConstraintDefinition _secondConstraintSecondDefinition;

    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _firstConstraint = Substitutes.UniqueFor<IConstraint>();
        _secondConstraint = Substitutes.UniqueFor<IConstraint>();
        _firstConstraintFirstDefinition = Substitute.For<IConstraintDefinition>();
        _firstConstraintFirstDefinition.Name.Returns((ConstraintName)"FirstConstraintFirstDefinition");
        _firstConstraintSecondDefinition = Substitute.For<IConstraintDefinition>();
        _firstConstraintSecondDefinition.Name.Returns((ConstraintName)"FirstConstraintSecondDefinition");
        _secondConstraintFirstDefinition = Substitute.For<IConstraintDefinition>();
        _secondConstraintFirstDefinition.Name.Returns((ConstraintName)"SecondConstraintFirstDefinition");
        _secondConstraintSecondDefinition = Substitute.For<IConstraintDefinition>();
        _secondConstraintSecondDefinition.Name.Returns((ConstraintName)"SecondConstraintSecondDefinition");
        _firstConstraint
            .When(_ => _.Define(Arg.Any<IConstraintBuilder>()))
            .Do(callInfo =>
            {
                var builder = callInfo.Arg<IConstraintBuilder>();
                builder.AddConstraint(_firstConstraintFirstDefinition);
                builder.AddConstraint(_firstConstraintSecondDefinition);
            });

        _secondConstraint
            .When(_ => _.Define(Arg.Any<IConstraintBuilder>()))
            .Do(callInfo =>
            {
                var builder = callInfo.Arg<IConstraintBuilder>();
                builder.AddConstraint(_secondConstraintFirstDefinition);
                builder.AddConstraint(_secondConstraintSecondDefinition);
            });

        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _clientArtifactsProvider.ConstraintTypes.Returns([_firstConstraint.GetType(), _secondConstraint.GetType()]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(_firstConstraint.GetType()).Returns(_firstConstraint);
        _serviceProvider.GetService(_secondConstraint.GetType()).Returns(_secondConstraint);

        _eventTypes = Substitute.For<IEventTypes>();

        _provider = new ConstraintsByBuilderProvider(_clientArtifactsProvider, _eventTypes, new DefaultNamingPolicy(), _serviceProvider);
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_provide_all_constraint_definitions() => _result.ShouldContainOnly(_firstConstraintFirstDefinition, _firstConstraintSecondDefinition, _secondConstraintFirstDefinition, _secondConstraintSecondDefinition);
}
