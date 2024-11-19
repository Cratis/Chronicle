// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler.given;

public class all_dependencies : Specification
{
    protected ReactorId Reactor_id;
    protected EventSequenceId event_sequence_id;
    protected Mock<IReactorInvoker> Reactor_invoker;
    protected Mock<ICausationManager> causation_manager;
    protected Mock<IServiceProvider> service_provider;

    void Establish()
    {
        Reactor_id = Guid.NewGuid().ToString();
        event_sequence_id = Guid.NewGuid().ToString();
        Reactor_invoker = new();
        causation_manager = new();
        service_provider = new();
    }
}
