// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class and_the_kernel_default_sink_changed : given_named_sinks
{
    async Task Establish() => await _manager.Register([Orders()]);

    Task Because()
    {
        // The kernel now names a default sink for read models; the client still sends the read model without a name.
        _options.Value = new ChronicleOptions { Sinks = new() { ReadModels = "reporting", Configurations = Sinks.Configurations } };
        return _manager.Register([Orders()]);
    }

    [Fact] void should_replay_into_the_new_sink() => _sinkChangeReplays.Received(1).ReplayFor(_eventStore, (ReadModelIdentifier)"orders");
}
