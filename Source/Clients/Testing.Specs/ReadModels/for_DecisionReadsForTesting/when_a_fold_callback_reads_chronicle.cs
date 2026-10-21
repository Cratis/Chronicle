// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_fold_callback_reads_chronicle : Specification
{
    static readonly AsyncLocal<Action?> _callback = new();
    EventStoreForTesting _store;
    EventSourceId _source;
    Exception _readError;
    Exception _appendError;
    DecisionRead<CallbackModel> _read;

    async Task Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterProjection<CallbackModel>(builder => builder.From<ModuleCreated>());
        _store = new(null, Defaults.Instance.ClientArtifactsProvider, options);
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Test"));
    }

    async Task Because()
    {
        var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        _callback.Value = () =>
        {
            _readError = Catch.Exception(() => _store.EventLog.GetTailSequenceNumber().GetAwaiter().GetResult());
            _appendError = Catch.Exception(() => _store.EventLog.Append(_source, new ModuleCreated("escape")).GetAwaiter().GetResult());
        };
        try
        {
            _read = await _store.GetDecisionReads().GetFor<CallbackModel>(unit, _source);
        }
        finally
        {
            _callback.Value = null;
        }
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_fold_the_model_outside_the_transport_allowance() => _read.Instance!.Name.ShouldEqual("Test");
    [Fact] void should_refuse_application_constructor_reads() => _readError.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_refuse_application_constructor_appends() => _appendError.ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();

    public class CallbackModel
    {
        public CallbackModel() => _callback.Value?.Invoke();
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
