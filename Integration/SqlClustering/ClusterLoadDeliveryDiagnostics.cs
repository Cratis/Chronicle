// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Cratis.Chronicle.Integration.SqlClustering;

/// <summary>
/// Fixture-local metadata capture. No payload, exception, formatter or arbitrary logger state is retained.
/// Fixed arrays and preallocated, count-capped indexes bound storage independently of run duration.
/// </summary>
internal sealed class ClusterLoadDeliveryDiagnostics : IDisposable
{
    internal const int RingCapacity = 4096;
    internal const int SummaryCapacity = 16384;
    const int OutputCapacity = 65536;
    readonly object _gate = new();
    readonly Guid _run = Guid.NewGuid();
    readonly Recent[] _ring = new Recent[RingCapacity];
    readonly Summary[] _summaries = new Summary[SummaryCapacity];
    readonly Batch[] _batches = new Batch[SummaryCapacity];
    readonly Dictionary<ulong, int> _sequenceIndex = new(SummaryCapacity);
    readonly Dictionary<(Guid Stream, long Batch), int> _batchIndex = new(SummaryCapacity);
    readonly AsyncLocal<(Guid Stream, long Batch)> _scope = new();
    long _records;
    long _sequenceDrops;
    long _batchDrops;
    long _rejected;
    long _spans;
    bool _seamSeen;
    bool _disposed;

    /// <summary>
    /// Gets the fixed storage upper bound, including conservative dictionary entry/bucket and object overhead.
    /// </summary>
    internal int StorageUpperBound => (Unsafe.SizeOf<Recent>() * RingCapacity) + (Unsafe.SizeOf<Summary>() * SummaryCapacity) +
        (Unsafe.SizeOf<Batch>() * SummaryCapacity) + (_sequenceIndex.EnsureCapacity(0) * 64) + (_batchIndex.EnsureCapacity(0) * 96) + 262144;

    /// <summary>
    /// Gets the fixed recent-record size (at most 128 bytes).
    /// </summary>
    internal int RecentRecordBytes => Unsafe.SizeOf<Recent>();

    /// <summary>
    /// Gets whether this async flow belongs to this fixture's enabled dispatch.
    /// </summary>
    internal bool HasScope => _scope.Value.Stream != Guid.Empty;

    /// <summary>
    /// Gets the retained sequence count.
    /// </summary>
    internal int SequenceCount
    {
        get
        {
            lock (_gate)
            {
                return _sequenceIndex.Count;
            }
        }
    }

    /// <summary>
    /// Gets the retained recent-record count.
    /// </summary>
    internal int RetainedRecords
    {
        get
        {
            lock (_gate)
            {
                return (int)Math.Min(_records, RingCapacity);
            }
        }
    }

    /// <summary>
    /// Wraps the existing synchronous enqueue exactly once. Cancellation and faults are rethrown unchanged.
    /// </summary>
    /// <param name="id">The synthetic fixture identity, parsed only by the fixture.</param>
    /// <param name="sequence">The context sequence.</param>
    /// <param name="enqueue">The original synchronous enqueue.</param>
    internal void Callback(Guid id, ulong sequence, Action enqueue)
    {
        var scope = _scope.Value;
        Record(new(20, scope.Stream, scope.Batch, Sequence: sequence, Identity: id));
        try
        {
            enqueue();
            Record(new(21, scope.Stream, scope.Batch, Sequence: sequence, Identity: id));
            Record(new(22, scope.Stream, scope.Batch, Sequence: sequence, Identity: id));
        }
        catch (OperationCanceledException)
        {
            Record(new(24, scope.Stream, scope.Batch, Sequence: sequence, Identity: id));
            throw;
        }
        catch
        {
            Record(new(23, scope.Stream, scope.Batch, Sequence: sequence, Identity: id));
            throw;
        }
    }

    /// <summary>
    /// Records one allowlisted primitive metadata record. Mutation and snapshots share one short lock.
    /// </summary>
    /// <param name="evidence">The sanitized record.</param>
    internal void Record(Evidence evidence)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _records++;
            var (trace, span) = CurrentTrace();
            _ring[(int)((_records - 1) % RingCapacity)] = new(_records, Stopwatch.GetTimestamp(), evidence.Stage, evidence.Stream, evidence.Batch, evidence.Delivery, evidence.Sequence, evidence.Previous, evidence.State, trace, span);
            evidence = evidence with { Trace = trace, Span = span };
            _seamSeen |= evidence.Stage == 1;
            if (evidence.Stage == 30) _spans++;
            RecordBatch(evidence);
            RecordSequence(evidence);
        }
    }

    /// <summary>
    /// Captures only scoped handle-span identifiers/status, never tags, exception events or baggage.
    /// </summary>
    /// <param name="stage">Start (30) or stop (31).</param>
    /// <param name="status">The numeric span status.</param>
    internal void Span(int stage, int status)
    {
        var scope = _scope.Value;
        if (scope.Stream != Guid.Empty)
        {
            Record(new(stage, scope.Stream, scope.Batch, State: status));
        }
    }

    /// <summary>
    /// Marks rejected diagnostic input as incomplete coverage, without retaining it.
    /// </summary>
    internal void Reject()
    {
        lock (_gate)
        {
            _rejected++;
        }
    }

    /// <summary>
    /// Correlates callback records to the exact awaited dispatch scope, without a global registry.
    /// </summary>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="batch">The batch ordinal.</param>
    /// <returns>A restoring scope.</returns>
    internal IDisposable BeginScope(Guid stream, long batch)
    {
        var previous = _scope.Value;
        _scope.Value = (stream, batch);
        return new Scope(_scope, previous);
    }

    /// <summary>
    /// Formats candidate evidence before recent chronology, bounded to 64 KiB including truncation markers.
    /// </summary>
    /// <param name="stored">Stored full identities.</param>
    /// <param name="observed">The final observed full identities.</param>
    /// <returns>The bounded failure-only report.</returns>
    internal string Describe(IEnumerable<(Guid Id, ulong Sequence)> stored, IEnumerable<(Guid Id, ulong Sequence)> observed)
    {
        var output = new BoundedOutput();
        var missing = 0;
        foreach (var candidate in stored.Where(candidate => !observed.Contains(candidate)))
        {
            missing++;
            if (missing <= 50)
            {
                DescribeCandidate(output, candidate.Id, candidate.Sequence, CaptureCandidate(candidate.Id, candidate.Sequence));
            }
        }
        output.Prepend(Header(missing));
        output.Add("Result publish-return is not kernel receipt or durable checkpoint. Redacted/non-GUID metadata is unknown. Missing retained evidence is not proof of non-delivery. Callback stages are a union, not per-delivery completion proof. Candidate/ring snapshots are individually atomic, not a global cut.\n");
        DescribeRecent(output);
        return output.Finish(missing > 50 ? missing - 50 : 0);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
        }
    }

    void RecordBatch(Evidence evidence)
    {
        if (evidence.Stream == Guid.Empty || evidence.Batch == 0)
        {
            return;
        }
        var key = (evidence.Stream, evidence.Batch);
        if (!_batchIndex.TryGetValue(key, out var index))
        {
            if (_batchIndex.Count == SummaryCapacity)
            {
                _batchDrops++;
                return;
            }
            index = _batchIndex.Count;
            _batchIndex.Add(key, index);
        }
        ApplyBatch(ref _batches[index], evidence);
    }

    static void ApplyBatch(ref Batch batch, Evidence evidence)
    {
        batch.Stream = evidence.Stream;
        batch.Ordinal = evidence.Batch;
        if (evidence.Stage < 20)
        {
            batch.Delivery = evidence.Delivery;
        }
        batch.Attempted |= evidence.Stage == 8;
        batch.Returned |= evidence.Stage == 9;
        batch.Cancelled |= evidence.Stage == 10;
        batch.Faulted |= evidence.Stage == 11;
        switch (evidence.Stage)
        {
            case 5:
                batch.Header = true;
                batch.Expected = evidence.Count;
                batch.Replay = evidence.State;
                batch.Partition = evidence.Partition;
                break;
            case 6: batch.Members++; break;
            case 8:
                batch.Checkpoint = evidence.Sequence;
                batch.State = evidence.State;
                break;
        }
    }

    void RecordSequence(Evidence evidence)
    {
        if (evidence.Sequence == ulong.MaxValue)
        {
            return;
        }
        if (!_sequenceIndex.TryGetValue(evidence.Sequence, out var index))
        {
            if (_sequenceIndex.Count == SummaryCapacity)
            {
                _sequenceDrops++;
                return;
            }
            index = _sequenceIndex.Count;
            _sequenceIndex.Add(evidence.Sequence, index);
        }
        ref var summary = ref _summaries[index];
        if (evidence.Stage == 6)
        {
            RecordMember(ref summary, evidence);
        }
        if (evidence.Stage == 7)
        {
            summary.Advanced = true;
            summary.Previous = evidence.Previous;
        }
        if (evidence.Stage >= 20)
        {
            RecordIdentity(ref summary, evidence);
        }
    }

    static (Guid Trace, ulong Span) CurrentTrace()
    {
        if (Activity.Current is not { } activity) return default;
        Span<byte> traceBytes = stackalloc byte[16];
        Span<byte> spanBytes = stackalloc byte[8];
        activity.TraceId.CopyTo(traceBytes);
        activity.SpanId.CopyTo(spanBytes);
        return (new Guid(traceBytes, bigEndian: true), BinaryPrimitives.ReadUInt64BigEndian(spanBytes));
    }

    static void RecordMember(ref Summary summary, Evidence evidence)
    {
        if (!summary.Member)
        {
            summary.Member = true;
            summary.FirstMember = evidence;
        }
        else if (summary.LastStream != evidence.Stream || summary.LastBatch != evidence.Batch)
        {
            summary.MemberVariants++;
        }
        summary.LastStream = evidence.Stream;
        summary.LastBatch = evidence.Batch;
    }

    static void RecordIdentity(ref Summary summary, Evidence evidence)
    {
        ref var identity = ref summary.First;
        if (identity.Used && identity.Id != evidence.Identity)
        {
            identity = ref summary.Second;
            if (identity.Used && identity.Id != evidence.Identity)
            {
                summary.IdentityOverflow = true;
                return;
            }
        }
        identity.Used = true;
        identity.Id = evidence.Identity;
        identity.Stages |= (CallbackStages)(1 << (evidence.Stage - 20));
        if (evidence.Stage == 20)
        {
            identity.Entries++;
        }
        identity.Stream = evidence.Stream;
        identity.Batch = evidence.Batch;
        identity.Trace = evidence.Trace;
        identity.Span = evidence.Span;
    }

    string Header(int missing)
    {
        lock (_gate)
        {
            var incomplete = !_seamSeen || _sequenceDrops != 0 || _batchDrops != 0 || _rejected != 0;
            return $"delivery diagnostics: run={_run} reactor=cluster-integrity store/namespace=unknown; missing={missing}, first=50, coverage={(incomplete ? "unknown" : "retained")}, handle-spans={_spans}, recent-record-bytes={RecentRecordBytes}, sequence-slots={_sequenceIndex.Count}/{SummaryCapacity}, batch-slots={_batchIndex.Count}/{SummaryCapacity}, sequence-drops={_sequenceDrops}, batch-drops={_batchDrops}, rejected={_rejected}, ring={Math.Max(1, _records - RingCapacity + 1)}..{_records}, evicted={Math.Max(0, _records - RingCapacity)}, storage-upper-bound={StorageUpperBound}\n";
        }
    }

    Candidate CaptureCandidate(Guid id, ulong sequence)
    {
        Span<ulong> other = stackalloc ulong[4];
        other.Clear();
        lock (_gate)
        {
            var count = CaptureOtherSequences(id, sequence, other);
            var found = _sequenceIndex.TryGetValue(sequence, out var index);
            var summary = found ? _summaries[index] : default;
            var firstFound = TryGetBatch(summary.FirstMember.Stream, summary.FirstMember.Batch, out var first);
            var lastFound = TryGetBatch(summary.LastStream, summary.LastBatch, out var last);
            return new(found, summary, firstFound, first, lastFound, last, other[0], other[1], other[2], other[3], count);
        }
    }

    bool TryGetBatch(Guid stream, long ordinal, out Batch batch)
    {
        var found = _batchIndex.TryGetValue((stream, ordinal), out var index);
        batch = found ? _batches[index] : default;
        return found;
    }

    static void DescribeCandidate(BoundedOutput output, Guid id, ulong sequence, Candidate candidate)
    {
        output.Add($"candidate id={id} sequence={sequence}\n");
        DescribeOtherSequences(output, candidate);
        if (!candidate.Found)
        {
            output.Add("  no retained sequence summary; coverage=unknown\n");
            return;
        }
        var summary = candidate.Summary;
        DescribeIdentity(output, summary.First, id);
        DescribeIdentity(output, summary.Second, id);
        output.Add($"  boundary-hint={Classify(summary, id)}\n");
        output.Add($"  identity-overflow={summary.IdentityOverflow}, membership-variants={summary.MemberVariants + (summary.Member ? 1 : 0)}, history-coverage={(summary.IdentityOverflow || summary.MemberVariants > 1 ? "unknown" : "retained")}, advanced={summary.Advanced}, previous={summary.Previous}\n");
        if (!summary.Member)
        {
            output.Add("  no retained client member; selection/checkpoint coverage question, origin unlocalized\n");
            return;
        }
        var member = summary.FirstMember;
        output.Add($"  member stream={member.Stream} batch={member.Batch} delivery={member.Delivery} type={member.Type} source={member.Source} correlation={member.Correlation} redacted={member.Redacted}\n");
        DescribeBatch(output, candidate.FirstBatchFound, candidate.FirstBatch);
        if (summary.LastStream != member.Stream || summary.LastBatch != member.Batch)
        {
            DescribeBatch(output, candidate.LastBatchFound, candidate.LastBatch);
        }
    }

    static string Classify(Summary summary, Guid id)
    {
        var identity = ExactIdentity(summary, id);
        if (HasStages(identity, CallbackStages.Enqueued | CallbackStages.Exit)) return "test-instance/snapshot inconsistency to investigate (not proof of queue loss)";
        if (HasStages(identity, CallbackStages.Entry)) return "callback entry retained; inspect fault/cancel/exit stages";
        if (summary.First.Used || summary.Second.Used) return "identity mismatch; inspect exact tuples";
        return summary.Member ? "dispatch/deserialization/callback boundary (negative entry evidence may be incomplete)" : "client-delivery/selection/checkpoint coverage question; origin unlocalized";
    }

    static Identity ExactIdentity(Summary summary, Guid id)
    {
        if (summary.First.Used && summary.First.Id == id) return summary.First;
        if (summary.Second.Used && summary.Second.Id == id) return summary.Second;
        return default;
    }

    static bool HasStages(Identity identity, CallbackStages stages) => identity.Used && identity.Stages.HasFlag(stages);

    static void DescribeIdentity(BoundedOutput output, Identity identity, Guid expected)
    {
        if (!identity.Used)
        {
            return;
        }
        output.Add($"  callback id={identity.Id} {(identity.Id == expected ? "exact-id" : "same-sequence-other-id")} stages={identity.Stages} entries={identity.Entries} last-stream={identity.Stream} last-batch={identity.Batch} trace={identity.Trace:N} span={identity.Span:x16}\n");
    }

    int CaptureOtherSequences(Guid id, ulong sequence, Span<ulong> output)
    {
        var retained = 0;
        foreach (var entry in _sequenceIndex)
        {
            ref var summary = ref _summaries[entry.Value];
            if (entry.Key != sequence && HasIdentity(summary, id))
            {
                output[retained++] = entry.Key;
                if (retained == output.Length)
                {
                    return retained;
                }
            }
        }
        return retained;
    }

    static void DescribeOtherSequences(BoundedOutput output, Candidate candidate)
    {
        ReadOnlySpan<ulong> other = [candidate.Other1, candidate.Other2, candidate.Other3, candidate.Other4];
        for (var index = 0; index < candidate.OtherCount; index++)
        {
            output.Add($"  same-id-other-sequence={other[index]}\n");
        }
    }

    static bool HasIdentity(Summary summary, Guid id) => summary.First.Id == id || summary.Second.Id == id;

    static void DescribeBatch(BoundedOutput output, bool found, Batch batch)
    {
        if (!found)
        {
            output.Add("  batch summary unavailable; coverage=unknown\n");
            return;
        }
        output.Add($"  batch stream={batch.Stream} ordinal={batch.Ordinal} delivery={batch.Delivery} partition={batch.Partition} members={batch.Members}/{batch.Expected} membership-complete={batch.Header && batch.Members == batch.Expected} replay={batch.Replay} publish-attempt={batch.Attempted} publish-return={batch.Returned} state={batch.State} checkpoint={batch.Checkpoint} cancelled={batch.Cancelled} faulted={batch.Faulted}\n");
    }

    void DescribeRecent(BoundedOutput output)
    {
        var recent = new Recent[128];
        long total;
        lock (_gate)
        {
            total = _records;
            var start = Math.Max(1, total - 127);
            for (var ordinal = start; ordinal <= total; ordinal++)
            {
                recent[ordinal - start] = _ring[(int)((ordinal - 1) % RingCapacity)];
            }
        }
        output.Add($"recent chronology: retained={Math.Min(total, RingCapacity)}, showing-last=128, omitted={Math.Max(0, Math.Min(total, RingCapacity) - 128)}\n");
        for (var index = 0; index < Math.Min(total, 128); index++)
        {
            var record = recent[index];
            output.Add($"  #{record.Ordinal} ticks={record.Timestamp} stage={record.Stage} stream={record.Stream} batch={record.Batch} delivery={record.Delivery} seq={record.Sequence} previous={record.Previous} state={record.State} trace={record.Trace:N} span={record.Span:x16}\n");
        }
    }

    /// <summary>
    /// The complete allowlisted primitive record. No references to external objects are retained.
    /// </summary>
    /// <param name="Stage">The numeric stage.</param>
    /// <param name="Stream">The stream incarnation.</param>
    /// <param name="Batch">The stream-local ordinal.</param>
    /// <param name="Delivery">The delivery identifier.</param>
    /// <param name="Sequence">The sequence or checkpoint.</param>
    /// <param name="Previous">The previous checkpoint.</param>
    /// <param name="State">The result or replay state.</param>
    /// <param name="Count">The member count.</param>
    /// <param name="Partition">The GUID partition, otherwise empty.</param>
    /// <param name="Type">The GUID event type, otherwise empty.</param>
    /// <param name="Source">The GUID source, otherwise empty.</param>
    /// <param name="Correlation">The correlation identifier.</param>
    /// <param name="Redacted">The redaction flags.</param>
    /// <param name="Identity">The synthetic fixture identity.</param>
    /// <param name="Trace">The current trace identifier.</param>
    /// <param name="Span">The current span identifier.</param>
    [StructLayout(LayoutKind.Auto)]
    internal readonly record struct Evidence(int Stage, Guid Stream = default, long Batch = 0, ulong Delivery = 0, ulong Sequence = ulong.MaxValue, ulong Previous = ulong.MaxValue, int State = 0, int Count = 0, Guid Partition = default, Guid Type = default, Guid Source = default, Guid Correlation = default, int Redacted = 0, Guid Identity = default, Guid Trace = default, ulong Span = 0);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Candidate(bool Found, Summary Summary, bool FirstBatchFound, Batch FirstBatch, bool LastBatchFound, Batch LastBatch, ulong Other1, ulong Other2, ulong Other3, ulong Other4, int OtherCount);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Recent(long Ordinal, long Timestamp, int Stage, Guid Stream, long Batch, ulong Delivery, ulong Sequence, ulong Previous, int State, Guid Trace, ulong Span);

    [Flags]
    enum CallbackStages
    {
        None = 0,
        Entry = 1,
        Enqueued = 2,
        Exit = 4,
        Fault = 8,
        Cancelled = 16
    }

    struct Identity
    {
        internal bool Used;
        internal Guid Id;
        internal Guid Trace;
        internal ulong Span;
        internal CallbackStages Stages;
        internal int Entries;
        internal Guid Stream;
        internal long Batch;
    }

    struct Summary
    {
        internal bool Member;
        internal Evidence FirstMember;
        internal Guid LastStream;
        internal long LastBatch;
        internal int MemberVariants;
        internal Identity First;
        internal Identity Second;
        internal bool IdentityOverflow;
        internal bool Advanced;
        internal ulong Previous;
    }

    struct Batch
    {
        internal Guid Stream;
        internal long Ordinal;
        internal ulong Delivery;
        internal Guid Partition;
        internal bool Header;
        internal int Expected;
        internal int Members;
        internal int Replay;
        internal bool Attempted;
        internal bool Returned;
        internal int State;
        internal ulong Checkpoint;
        internal bool Cancelled;
        internal bool Faulted;
    }

    sealed class Scope(AsyncLocal<(Guid Stream, long Batch)> slot, (Guid Stream, long Batch) previous) : IDisposable
    {
        public void Dispose() => slot.Value = previous;
    }

    sealed class BoundedOutput
    {
        const int MarkerReserve = 256;
        const int HeaderReserve = 1024;
        readonly StringBuilder _text = new();
        int _bytes = HeaderReserve;
        int _omitted;

        internal void Add(string text)
        {
            var bytes = Encoding.UTF8.GetByteCount(text);
            if (_bytes + bytes > OutputCapacity - MarkerReserve)
            {
                _omitted++;
                return;
            }
            _text.Append(text);
            _bytes += bytes;
        }

        internal void Prepend(string text)
        {
            // Reserve the fixed-size header before candidate text can consume the output budget.
            _bytes -= HeaderReserve;
            if (_bytes + Encoding.UTF8.GetByteCount(text) > OutputCapacity - MarkerReserve)
            {
                _omitted++;
                return;
            }
            _text.Insert(0, text);
            _bytes += Encoding.UTF8.GetByteCount(text);
        }

        internal string Finish(int candidatesOmitted)
        {
            _text.Append($"truncation: candidate-details-omitted={candidatesOmitted}, lines-omitted={_omitted}\n");
            return _text.ToString();
        }
    }
}
