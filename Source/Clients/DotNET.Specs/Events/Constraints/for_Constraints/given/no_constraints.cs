// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
