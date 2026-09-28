// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_staging_after_a_compatible_unit_is_completed : Specification
{
    ServiceProvider _services;
    ILogger<UnitOfWork> _logger;
    CorrelationId _correlationId;
    Exception _error;

    void Establish()
    {
        _logger = Substitute.For<ILogger<UnitOfWork>>();
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        var services = new ServiceCollection();
        services.AddSingleton(_logger);
        _services = services.BuildServiceProvider();
        _correlationId = CorrelationId.New();
    }

    async Task Because()
    {
        var store = new EventStoreForTesting(_services, Substitute.For<IClientArtifactsProvider>());
        var unit = store.UnitOfWorkManager.Begin(_correlationId);
        await unit.Rollback();
        _error = Record.Exception(() => unit.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown()));
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_preserve_compatibility_enrollment() => _error.ShouldBeNull();
    [Fact] void should_log_the_late_attempt_with_its_correlation_id() => _logger.ReceivedCalls()
        .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
        .Select(_ => _.GetArguments())
        .Count(_ => _[0] is LogLevel level && level == LogLevel.Error && _[2]!.ToString()!.Contains(_correlationId.ToString())).ShouldEqual(1);
}
