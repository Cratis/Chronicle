// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Integration.Clustering.for_Occurrences;
using Cratis.Chronicle.Integration.for_PerProcessActivation;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class RecoveryScenario : IAsyncDisposable
{
    readonly SqlProcessKernel _kernel = new();
    RecoverySession? _session;
    ChildProcess _holder = null!;
    Settlement _first = null!;
    OccurrenceSnapshotResponse _leased = null!;

    public static async Task<RecoveryScenario> Start()
    {
        var options = ChronicleRuntimeOptions.Parse();
        Assert.SkipUnless(options.Mode == ChronicleRuntimeMode.OutOfProcess && options.StorageProvider == ChronicleStorageProvider.PostgreSql,
            "Process-loss proofs run in the outofprocess postgresql client matrix lane.");
        var scenario = new RecoveryScenario();
        try
        {
            await scenario.Establish();
            return scenario;
        }
        catch
        {
            await scenario.DisposeAsync();
            throw;
        }
    }

    public async Task KillKernelAndRecover()
    {
        await _kernel.KillKernel();
        await AssertHolderAliveAtBarrier();
        await _kernel.RestartKernel();
        await _session!.DisposeAsync();
        _session = new(_kernel.Connection, _kernel.Store);
        await _session.Connect();
        await Recover();

        // The old application has never been stopped, disposed or released during kernel loss.
        await AssertHolderAliveAtBarrier();
    }

    public async Task KillApplicationAndRecover()
    {
        await _kernel.AssertKernelUnchanged();
        await _holder.Kill();
        await _kernel.AssertKernelUnchanged();
        await Recover();
        await _kernel.AssertKernelUnchanged();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_session is not null) await _session.DisposeAsync();
        }
        finally
        {
            try
            {
                if (_holder is not null) await _holder.DisposeAsync();
            }
            finally
            {
                await _kernel.DisposeAsync();
            }
        }
    }

    async Task Establish()
    {
        await _kernel.Start();
        _holder = await _kernel.StartApplication(hold: true);
        _session = new(_kernel.Connection, _kernel.Store);
        await _session.Connect();
        await _holder.Send("record");
        var recorded = await _holder.WaitForLine("RECORDED", TimeSpan.FromSeconds(30));
        var id = recorded["RECORDED ".Length..];
        _first = Settlement.Parse(await _holder.WaitForLine($"SETTLED {id}", TimeSpan.FromSeconds(90)));
        _first.Status.ShouldEqual("Recorded");
        _first.WinnerEpoch.ShouldEqual(_first.Epoch);
        _leased = await _session.WaitFor(id, row => row.State == OccurrenceState.Leased && row.Receipts.Count() == 1);
        _leased.Epoch.ShouldEqual(_first.Epoch);
        _leased.Receipts.Single().Receipt.Epoch.ShouldEqual(_first.Epoch);
        var events = await _session.Store.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, id).WaitAsync(TimeSpan.FromSeconds(30));
        events.Count.ShouldEqual(1);
        await AssertHolderAliveAtBarrier();
    }

    async Task Recover()
    {
        var replacement = await _kernel.StartApplication(hold: false);
        var recovered = Settlement.Parse(await replacement.WaitForLine($"SETTLED {_first.OccurrenceId}", TimeSpan.FromSeconds(180)));
        recovered.ProcessId.ShouldNotEqual(_first.ProcessId);
        recovered.Epoch.ShouldEqual(_first.Epoch + 1);
        recovered.Status.ShouldEqual("AlreadyRecorded");
        recovered.WinnerEpoch.ShouldEqual(_first.Epoch);
        var completed = await _session!.WaitFor(_first.OccurrenceId, row => row.State == OccurrenceState.Completed);
        completed.Attempts.ShouldEqual(2);
        await new OccurrenceRecoveryAssertions(_session.Store, _session.Service, RecoverySession.Trigger)
            .Recovered(_first.OccurrenceId, _first.Epoch, completed, $"{_first}; {recovered}");
        AssertSameWinningReceipt(completed);
        await replacement.Stop();
    }

    async Task AssertHolderAliveAtBarrier()
    {
        await _holder.Send("probe");
        var alive = await _holder.WaitForLine("ALIVE", TimeSpan.FromSeconds(10));
        alive.ShouldEqual($"ALIVE {_first.ProcessId} True");
    }

    void AssertSameWinningReceipt(OccurrenceSnapshotResponse completed)
    {
        var before = _leased.Receipts.Single().Receipt;
        var after = completed.Receipts.Single().Receipt;
        after.Id.ShouldEqual(before.Id);
        after.Epoch.ShouldEqual(before.Epoch);
        after.Outcome.ShouldEqual(Contracts.Storage.Occurrences.EffectOutcome.Appended);
        after.EventSequenceId.ShouldEqual(before.EventSequenceId);
        after.FirstSequenceNumber.ShouldEqual(before.FirstSequenceNumber);
        after.LastSequenceNumber.ShouldEqual(before.LastSequenceNumber);
        after.Response.ShouldEqual(before.Response);
        after.FirstSequenceNumber.ShouldEqual(after.LastSequenceNumber);
    }
}
