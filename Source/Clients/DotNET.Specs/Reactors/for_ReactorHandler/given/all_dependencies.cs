// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler.given;

public class all_dependencies : Specification
{
    protected ReactorId _reactorId;
    protected IEventStore _eventStore;
    protected EventSequenceId _eventSequenceId;
    protected IReactorInvoker _reactorInvoker;
    protected ICausationManager _causationManager;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _reactorId = Guid.NewGuid().ToString();
        _eventSequenceId = Guid.NewGuid().ToString();
        _reactorInvoker = Substitute.For<IReactorInvoker>();
        _causationManager = Substitute.For<ICausationManager>();
        _serviceProvider = Substitute.For<IServiceProvider>();
    }
}
