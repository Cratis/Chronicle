// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods.given;

public class a_seat_index_scenario : Specification
{
    protected ReactorScenario<SeatIndexReactor> _scenario;
    protected SeatBatchLog _log;

    void Establish()
    {
        _log = new();
        _scenario = new ReactorScenario<SeatIndexReactor>();
        _scenario.Services.AddSingleton(_log);
    }

    void Destroy() => _scenario.Dispose();
}
