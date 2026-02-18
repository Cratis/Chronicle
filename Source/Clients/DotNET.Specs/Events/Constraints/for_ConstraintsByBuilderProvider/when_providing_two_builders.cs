// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintsByBuilderProvider;

public class when_providing_two_builders : Specification
{
    ConstraintsByBuilderProvider _provider;
    IClientArtifactsProvider _clientArtifactsProvider;
    IServiceProvider _serviceProvider;
    IEventTypes _eventTypes;

    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _clientArtifactsProvider.ConstraintTypes.Returns([typeof(FirstTestConstraint), typeof(SecondTestConstraint)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        _provider = new ConstraintsByBuilderProvider(_clientArtifactsProvider, _eventTypes, new DefaultNamingPolicy(), _serviceProvider);
    }

    void Because() => _result = _provider.Provide();

    [Fact] void should_provide_four_constraint_definitions() => _result.Count.ShouldEqual(4);
    [Fact] void should_contain_first_constraint_definitions() => _result.ShouldContain(_ => _.Name == (ConstraintName)"FirstConstraintFirstDefinition");
    [Fact] void should_contain_second_constraint_definitions() => _result.ShouldContain(_ => _.Name == (ConstraintName)"SecondConstraintSecondDefinition");
}
