// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Orleans.Jobs;

using KernelObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class PatternCaptureCalls(PatternCaptureControl control) : IOutgoingGrainCallFilter
{
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (context.SourceContext?.GrainInstance?.GetType().FullName == PatternCaptureControl.EventSequenceGrainType && context.SourceContext.GrainId.Key.ToString() == control.Key.ToString())
        {
            control.EventSequenceContext = context.SourceContext;
        }

        var observerKey = new ObserverKey(PatternCapture.ObserverIdentifier, control.Key.EventStore, control.Key.Namespace, control.Key.EventSequenceId);
        if (context.SourceContext?.GrainInstance is Observer observer && context.SourceContext.GrainId.Key.ToString() == observerKey.ToString() && context.InterfaceMethod.Name == nameof(IJobsManager.GetAllJobs))
        {
            control.InitializationAttempts++;
            if (control.FailInitialization)
            {
                control.FailInitialization = false;

                // ResumeJobs runs after _subscription is assigned but before Subscribe finishes.
                (await observer.GetSubscription()).IsSubscribed.ShouldBeTrue();
                control.InitializationStarted.TrySetResult();
                await control.InitializationReleased.Task;
                throw new IOException("Subscription initialization failed after assignment.");
            }
        }

        var invocation = context.Invoke();
        if (context.TargetId.Key.ToString() == observerKey.ToString() && context.InterfaceMethod.Name == nameof(KernelObserver.EnsureSubscribed))
        {
            control.EnsureRequested.TrySetResult();
        }
        await invocation;
    }
}
