// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Moq;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceQueue.when_recording;

public class an_occurrence : given.a_queue_grain
{
    async Task Because() => await _grain.Record("durable", "{\"value\":1}", OccurrenceKind.Application);

    [Fact] void should_install_the_durable_wake_reminder() => _silo.ReminderRegistry.Mock.Verify(
        registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "occurrence-wake", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)),
        Times.Once);

    [Fact] void should_keep_a_one_second_timer_alongside_the_reminder() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
