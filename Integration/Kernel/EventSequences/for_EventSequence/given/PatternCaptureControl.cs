// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class PatternCaptureControl
{
    public EventSequenceKey Key { get; private set; } = EventSequenceKey.NotSet;
    public bool HoldSubscription { get; private set; }
    public bool FailStateWrite { get; private set; }
    public int FailedStateWrites { get; private set; }
    public TaskCompletionSource SubscriptionStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SubscriptionReleased { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SubscriptionCompleted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Begin(EventSequenceKey key, bool holdSubscription = false, bool failStateWrite = false)
    {
        Key = key;
        HoldSubscription = holdSubscription;
        FailStateWrite = failStateWrite;
        FailedStateWrites = 0;
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
