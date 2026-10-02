// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class PatternCaptureControl
{
    /// <summary>
    /// Kernel and client intentionally share this full name, so typeof is ambiguous in integration specs.
    /// </summary>
    public const string EventSequenceGrainType = "Cratis.Chronicle.EventSequences.EventSequence";
    public EventSequenceKey Key { get; private set; } = EventSequenceKey.NotSet;
    public bool HoldSubscription { get; private set; }
    public bool FailStateWrite { get; private set; }
    public int FailedStateWrites { get; private set; }
    public bool FailSubscription { get; set; }
    public bool FailInitialization { get; set; }
    public int SubscriptionAttempts { get; set; }
    public int InitializationAttempts { get; set; }
    public IGrainContext? EventSequenceContext { get; set; }
    public TaskCompletionSource InitializationStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource InitializationReleased { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource EnsureRequested { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource RetriedBeyondCollectionAge { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SubscriptionStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SubscriptionReleased { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SubscriptionCompleted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Begin(EventSequenceKey key, bool holdSubscription = false, bool failStateWrite = false)
    {
        Key = key;
        HoldSubscription = holdSubscription;
        FailStateWrite = failStateWrite;
        FailedStateWrites = 0;
        FailSubscription = false;
        FailInitialization = false;
        SubscriptionAttempts = 0;
        InitializationAttempts = 0;
        EventSequenceContext = null;
        InitializationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InitializationReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EnsureRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RetriedBeyondCollectionAge = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SubscriptionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SubscriptionReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SubscriptionCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public bool ShouldFailStateWrite(EventSequenceKey key)
    {
        if (!FailStateWrite || key != Key)
        {
            return false;
        }

        FailStateWrite = false;
        FailedStateWrites++;
        return true;
    }
}
