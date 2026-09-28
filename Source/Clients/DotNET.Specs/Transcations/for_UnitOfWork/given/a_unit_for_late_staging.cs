// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_for_late_staging : a_unit_of_work
{
    protected ILogger<UnitOfWork> _logger;
    protected object _original;
    protected Exception _eventError;
    protected Exception _batchError;
    protected bool _batchWasEnumerated;
    protected virtual UnitOfWorkLifecyclePolicy Policy => UnitOfWorkLifecyclePolicy.Compatibility;

    void Establish()
    {
        _logger = Substitute.For<ILogger<UnitOfWork>>();
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        _unitOfWork = new UnitOfWork(_correlationId, OnUnitOfWorkCompleted, _eventStore, Policy, _logger);
        _original = new object();
        _unitOfWork.AddEvent(EventSequenceId.Log, "original", _original, Causation.Unknown());
    }

    protected void AttemptLateStaging()
    {
        _eventError = Record.Exception(() => _unitOfWork.AddEvent(EventSequenceId.Log, "late", new PrivatePayload(), Causation.Unknown()));
        _batchError = Record.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, LazyEvents(), []));
    }

    protected string[] GetErrorLogs() => _logger.ReceivedCalls()
        .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
        .Select(_ => _.GetArguments())
        .Where(_ => _[0] is LogLevel level && level == LogLevel.Error)
        .Select(_ => _[2]!.ToString()!)
        .ToArray();

    IEnumerable<EventForEventSourceId> LazyEvents()
    {
        _batchWasEnumerated = true;
        yield return new EventForEventSourceId("late", new PrivatePayload(), Causation.Unknown());
    }

    sealed class PrivatePayload
    {
        public override string ToString() => "PRIVATE_PAYLOAD";
    }
}
