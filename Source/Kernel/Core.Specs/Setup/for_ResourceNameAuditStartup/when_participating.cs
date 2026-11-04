// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.for_ResourceNameAuditStartup;

public class when_participating : Specification
{
    ISiloLifecycle _lifecycle;
    ResourceNameAuditStartup _startup;

    void Establish()
    {
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _startup = new(
            new(
                [],
                ResourceNameSnapshot.NotReady(),
                Substitute.For<IResourceNameGate>(),
                ResourceNameExemptions.None,
                NullLogger<ResourceNameAudit>.Instance,
                TimeProvider.System),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorageErrorClassifier>());
    }

    void Because() => _startup.Participate(_lifecycle);

    [Fact] void should_subscribe_after_the_first_stage_and_before_the_services_become_active() =>
        _lifecycle.Received(1).Subscribe(nameof(ResourceNameAuditStartup), ServiceLifecycleStage.ApplicationServices, Arg.Any<ILifecycleObserver>());
}
