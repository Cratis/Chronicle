// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

public class a_read_model_scenario<TReactor> : Specification
    where TReactor : class, IReactor
{
    protected ReactorScenario<TReactor> _scenario;
    protected IRecordScenarioReadModel _recorder;
    protected EventSourceId _source;
    protected Defaults _defaults;
    protected Exception? _exception;

    void Establish()
    {
        _source = "triggering-source";
        _recorder = Substitute.For<IRecordScenarioReadModel>();
        _defaults = new(new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(VibeStarted)],
            ModelBoundProjections = [typeof(ScenarioReadModel), typeof(VibeAttendees)]
        });
        _scenario = new(_defaults);
        _scenario.Services.AddSingleton(_recorder);
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
