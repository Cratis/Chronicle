// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_a_local_reducer_callback_arrives_for_a_kernel_observed_reducer : given.a_reducer_backed_model
{
    void Because()
    {
        Watch();
        SendLocalChanges();
    }

    [Fact] void should_not_notify_the_watcher() => _changes.ShouldBeEmpty();
    [Fact] void should_not_subscribe_locally() => _local.HasObservers.ShouldBeFalse();
}
