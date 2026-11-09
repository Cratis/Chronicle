// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_resumed_without_having_started : Specification
{
    IEventStore _eventStore;
    ReadModelReactors _reactors;
    IClientArtifactsProvider _artifacts;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _reactors = new ReadModelReactors(
            _eventStore,
            _artifacts,
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IReadModelReactorInvoker>(),
            new JsonSerializerOptions(),
            Substitute.For<ILogger<ReadModelReactors>>());
    }

    void Because() => _reactors.ResumeAfterReconnect();

    [Fact] void should_not_discover_read_model_reactors() => _ = _artifacts.DidNotReceive().ReadModelReactors;
}
