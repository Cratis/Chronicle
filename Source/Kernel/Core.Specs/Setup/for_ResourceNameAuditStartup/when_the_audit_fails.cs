// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.for_ResourceNameAuditStartup;

public class when_the_audit_fails : Specification
{
    ISiloLifecycle _lifecycle;
    ResourceNameSnapshot _snapshot;
    ILifecycleObserver _observer;
    Exception _result;

    void Establish()
    {
        var inventory = Substitute.For<IResourceInventory>();
        inventory.Read(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<IEnumerable<ResourceUse>>(new InvalidOperationException()));
        _snapshot = ResourceNameSnapshot.NotReady();
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _lifecycle
            .When(_ => _.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()))
            .Do(call => _observer = call.ArgAt<ILifecycleObserver>(2));

        new ResourceNameAuditStartup(
            new([inventory], _snapshot, Substitute.For<IResourceNameGate>(), ResourceNameExemptions.None, NullLogger<ResourceNameAudit>.Instance, TimeProvider.System),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorageErrorClassifier>()).Participate(_lifecycle);
    }

    async Task Because() => _result = await Catch.Exception(() => _observer.OnStart(CancellationToken.None));

    [Fact] void should_fault_the_start() => _result.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_leave_the_names_unavailable() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
}
