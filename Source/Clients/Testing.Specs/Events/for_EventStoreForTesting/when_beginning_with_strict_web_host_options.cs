// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_beginning_with_strict_web_host_options : Specification
{
    WebApplication _app;
    Exception _error;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisChronicle(_ =>
        {
            _.EventStore = "testing";
            _.UnitOfWorkLifecyclePolicy = UnitOfWorkLifecyclePolicy.Strict;
        });
        _app = builder.Build();
    }

    async Task Because()
    {
        var store = new EventStoreForTesting(_app.Services, Substitute.For<IClientArtifactsProvider>());
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        await unit.Rollback();
        _error = Record.Exception(() => unit.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown()));
    }

    async Task Destroy() => await _app.DisposeAsync();

    [Fact] void should_use_the_host_options_policy() => _error.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
}
