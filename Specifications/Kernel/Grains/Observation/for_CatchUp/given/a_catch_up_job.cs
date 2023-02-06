// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp.given;

public class a_catch_up_job : GrainSpecification
{
    protected Mock<IPersistentState<ObserverState>> persistent_state;
    protected ObserverState state;
    protected ObserverState state_on_write;
    protected CatchUp catch_up;

    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected Mock<IObserverSupervisor> supervisor;

    protected override Grain GetGrainInstance()
    {
        persistent_state = new();
        persistent_state.Setup(_ => _.State).Returns(() => state);
        persistent_state.Setup(_ => _.WriteStateAsync()).Returns(() =>
        {
            var serialized = JsonSerializer.Serialize(state);
            state_on_write = JsonSerializer.Deserialize<ObserverState>(serialized);
            return Task.CompletedTask;
        });

        supervisor = new();
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);

        event_sequence_storage_provider = new();

        catch_up = new CatchUp(
            Mock.Of<IExecutionContextManager>(),
            () => event_sequence_storage_provider.Object,
            persistent_state.Object,
            Mock.Of<ILogger<CatchUp>>());

        return catch_up;
    }
}
