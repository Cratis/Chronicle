// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// Two observers that have each handled one event and been released, in a kernel whose safety net looks every few seconds.
/// </summary>
/// <param name="fixture">The fixture.</param>
public class two_dormant_observers(DormantSafetyNetFixture fixture) : a_dormant_reactor<DormantSafetyNetFixture>(fixture)
{
    protected IReactorHandler _alpha;
    protected IReactorHandler _beta;
    protected EventSequenceNumber _alphaEvent;
    protected EventSequenceNumber _betaEvent;

    protected GrainCallRecorder Calls => ChronicleFixture.Calls;
    protected DormantAlphaReactor Alpha => ChronicleFixture.Alpha;
    protected DormantBetaReactor Beta => ChronicleFixture.Beta;

    protected override void ConfigureObserverSettings(Dictionary<string, string?> settings)
    {
        // The check of a dormant observer is due between six and nine seconds after it became dormant. The unconditional
        // wake is an hour away, so a wake in these specifications can only come from the observer being behind.
        settings["Observers:DormantSafetyNet"] = "10";
        settings["Observers:DormantSafetyNetFullWake"] = "3600";
    }

    protected override void ConfigureSiloServices(IServiceCollection services) => services.AddSingleton<IIncomingGrainCallFilter>(Calls);

    async Task Establish()
    {
        _alpha = _store.Reactors.GetHandlerFor<DormantAlphaReactor>();
        _beta = _store.Reactors.GetHandlerFor<DormantBetaReactor>();
        await _alpha.WaitTillActive(TimeSpan.FromSeconds(30));
        await _beta.WaitTillActive(TimeSpan.FromSeconds(30));

        _alphaEvent = (await _store.EventLog.Append("alpha", new DormantAlphaEvent("first"))).SequenceNumber;
        _betaEvent = (await _store.EventLog.Append("beta", new DormantBetaEvent("first"))).SequenceNumber;
        await WaitForHandled(Alpha.Handled, 1);
        await WaitForHandled(Beta.Handled, 1);

        // A new observer registers a reminder for its alerts that is due a minute or two later and that wakes it once more,
        // so what is measured starts when both have been released and have stayed released.
        await WaitUntilReleased(_alpha);
        await WaitUntilReleased(_beta);
        await WaitUntilNoneIsResident([_alpha, _beta]);
    }

    protected int AlertCalls(IReactorHandler handler) => Calls.Count(nameof(Cratis.Chronicle.Observation.Alerts.IObserverAlerts), null, handler.Id.Value);

    protected int SafetyNetWakes(IReactorHandler handler) => Calls.Count(nameof(Cratis.Chronicle.Observation.IObserver), nameof(Cratis.Chronicle.Observation.IObserver.RunWatchdogOnce), handler.Id.Value);
}
