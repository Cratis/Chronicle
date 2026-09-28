// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_beginning_with_base_options_configured_strict : Specification
{
    ServiceProvider _services;
    Exception _error;

    void Establish()
    {
        var services = new ServiceCollection();
        services.Configure<ChronicleOptions>(_ => _.UnitOfWorkLifecyclePolicy = UnitOfWorkLifecyclePolicy.Strict);
        _services = services.BuildServiceProvider();
    }

    async Task Because()
    {
        var store = new EventStoreForTesting(_services, Substitute.For<IClientArtifactsProvider>());
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        await unit.Rollback();
        _error = Record.Exception(() => unit.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown()));
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_use_the_base_options_policy() => _error.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
}
