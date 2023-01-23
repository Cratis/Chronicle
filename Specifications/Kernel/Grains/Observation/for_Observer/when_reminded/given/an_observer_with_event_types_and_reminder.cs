// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_reminded.given;

public class an_observer_with_event_types_and_reminder : for_Observer.given.an_observer_and_two_event_types
{
    protected Mock<IGrainReminder> reminder;

    void Establish()
    {
        reminder = new();
        reminder_registry.Setup(_ => _.GetReminder(Observer.RecoverReminder)).Returns(Task.FromResult(reminder.Object));
    }
}
