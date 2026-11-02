// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;
using Cratis.Json;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

public class a_borrowed_store : appended_contexts
{
    readonly List<IDisposable> _scenarios = [];
    protected EventStoreForTesting _store;

    void Establish() => _store = new EventStoreForTesting();

    void Destroy()
    {
        foreach (var scenario in _scenarios)
        {
            scenario.Dispose();
        }

        _store.Dispose();
    }

    protected ReadModelScenario<TReadModel> Borrowed<TReadModel>(IServiceProvider? serviceProvider = null)
        where TReadModel : class => Track(_store.CreateReadModelScenario<TReadModel>(serviceProvider));

    protected ReadModelScenario<TReadModel> Fresh<TReadModel>()
        where TReadModel : class => Track(new ReadModelScenario<TReadModel>());

    protected static string Json(object? value) => JsonSerializer.Serialize(value, Globals.JsonSerializerOptions);

    ReadModelScenario<TReadModel> Track<TReadModel>(ReadModelScenario<TReadModel> scenario)
        where TReadModel : class
    {
        _scenarios.Add(scenario);
        return scenario;
    }
}
