// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_choosing_how_to_observe.given;

/// <summary>
/// Services whose reactors and reducers each have a stream, and a count of how often the multiplexed observation was asked for.
/// </summary>
public class services_with_a_stream_per_observer : Specification
{
    protected Services _perObserver;
    protected int _multiplexedRequested;

    void Establish()
    {
        var parameters = typeof(Services).GetConstructors().Single().GetParameters();
        _perObserver = (Services)typeof(Services).GetConstructors().Single()
            .Invoke([.. parameters.Select(_ => Substitute.For([_.ParameterType], []))]);
    }

    protected Services Choose(bool enabled, params string[] capabilities) =>
        ChronicleConnection.ChooseObservation(
            _perObserver,
            enabled,
            capabilities,
            () =>
            {
                _multiplexedRequested++;
                return Substitute.For<IMultiplexedObservation>();
            },
            NullLoggerFactory.Instance);
}
