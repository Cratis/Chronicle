// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;
using context = Cratis.Chronicle.Kernel.Integration.for_PatternCapture.when_a_namespace_captures_patterns.and_capture_is_off.context;

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.when_a_namespace_captures_patterns;

/// <summary>
/// With pattern capture off (#6118) a namespace that is added and used creates no capture observer and no miner: nothing is
/// subscribed, no definition is stored and no grain of either kind activates.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.PatternCaptureOffCollection.Name)]
public class and_capture_is_off(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureOffFixture fixture) : given.a_namespace_without_pattern_capture(fixture)
    {
        public bool CaptureWasRegistered;
        public int CaptureObservers;
        public int Miners;

        async Task Because()
        {
            await _store.EventLog.Append("source", new for_DormantObserver.given.DormantObserverEvent("first"));
            await WaitForHandled(1);

            // Subscribing happens when a namespace is added and when event types are registered, both of which are
            // long done by now; the settle time covers the reconcile that the event log runs when it activates.
            await Task.Delay(TimeSpan.FromSeconds(15));

            CaptureWasRegistered = await Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).Reactors.Has(PatternCapture.ObserverIdentifier);
            CaptureObservers = (await Resident(ObserverGrainPrefix, PatternCapture.ObserverIdentifier)).Length;
            Miners = (await Resident("patternminer", string.Empty)).Length;
        }
    }

    [Fact] void should_not_store_a_capture_definition() => Context.CaptureWasRegistered.ShouldBeFalse();
    [Fact] void should_not_activate_a_capture_observer() => Context.CaptureObservers.ShouldEqual(0);
    [Fact] void should_not_activate_a_miner() => Context.Miners.ShouldEqual(0);
}
