// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class all_dependencies : Specification
{
    protected IAggregateRootEventHandlers _eventHandlers;
    protected IEventSequence _eventSequence;
    protected ICausationManager _causationManager;
    protected IAggregateRootMutation _mutation;
    protected IAggregateRootMutator _mutator;

    void Establish()
    {
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();
        _eventSequence = Substitute.For<IEventSequence>();
        _causationManager = Substitute.For<ICausationManager>();
        _mutation = Substitute.For<IAggregateRootMutation>();
        _mutator = Substitute.For<IAggregateRootMutator>();
        _mutation.Mutator.Returns(_mutator);
    }
}
