// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes;

public class and_the_notifier_activation_is_lost : given.a_composed_watch_topology
{
    Watch _watch;

    void Establish() => _readModels.WatchRegistrationRefreshInterval = TimeSpan.FromMilliseconds(50);

    async Task Because()
    {
        _watch = await StartWatch(StoreS, NamespaceA);
        var producer = await given.ProjectionProducer.Create(_router, ObserverId, StoreS, NamespaceA);
        await _router.ReplaceAll();

        // The fresh activations hold no registration until the watch re-registers; produce once it has.
        await _router.SubscribedSinceReplace.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await producer.Produce("after-loss");
    }

    [Fact] void should_deliver_a_change_after_the_watch_re_registers() => Values(_watch).ShouldEqual(["after-loss"]);
}
