// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_StoreEntitlementsStartupNotice;

/// <summary>
/// The notice reads the grants the startup task loaded, so it runs as part of the silo start, after the silo is active, and never
/// as a hosted service that runs before the listeners bind (#4040).
/// </summary>
public class when_participating : Specification
{
    ISiloLifecycle _lifecycle;
    StoreEntitlementsStartupNotice _notice;

    void Establish()
    {
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _notice = new(Options.Create(new ChronicleOptions()), Substitute.For<IStoreEntitlementsCache>(), new RecordingLogger());
    }

    void Because() => _notice.Participate(_lifecycle);

    [Fact] void should_not_be_a_hosted_service() => (_notice is IHostedService).ShouldBeFalse();
    [Fact] void should_run_after_the_silo_is_active() => _lifecycle.Received(1).Subscribe(nameof(StoreEntitlementsStartupNotice), Arg.Is<int>(_ => _ > ServiceLifecycleStage.Active), Arg.Any<ILifecycleObserver>());
}
