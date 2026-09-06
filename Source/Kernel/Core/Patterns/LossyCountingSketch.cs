// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents a Lossy Counting sketch over candidate itemsets, holding bounded state no matter how many events
/// flow through it.
/// </summary>
/// <remarks>
/// <para>
/// Lossy Counting (Manku and Motwani) divides the stream into buckets of width one over the error parameter. An
/// itemset already in the sketch has its frequency incremented; a new one enters carrying the current bucket
/// number minus one as its error, which is the most it could have occurred before anyone was watching. At a bucket
/// boundary every itemset whose frequency plus error has not kept up with the bucket number is dropped.
/// </para>
/// <para>
/// The guarantee that buys: nothing with true frequency above the support threshold is ever missed, nothing
/// reported is off by more than the error parameter times the number of observations, and the number of retained
/// itemsets is bounded regardless of stream length. That last part is the point - the whole feature exists to
/// summarize an unbounded event history in memory that does not grow with it.
/// </para>
/// <para>
/// The sketch is not thread-safe. It is owned by whatever mines a single grouping scope, and that owner serializes
/// access - see <see cref="PatternMiner"/>.
/// </para>
/// </remarks>
public sealed class LossyCountingSketch
{
    readonly Dictionary<FacetSetKey, MutableEntry> _entries = [];
    readonly double _error;
    readonly double _decayFactor;
    readonly long _bucketWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="LossyCountingSketch"/> class.
    /// </summary>
    /// <param name="error">The error parameter, bounding how far a counted frequency may lag the true one.</param>
    /// <param name="decayFactor">The daily decay applied to the weight of an itemset that has gone unseen.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the error parameter is not between zero and one, exclusive.</exception>
    public LossyCountingSketch(double error, double decayFactor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(error, 0d);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(error, 1d);

        _error = error;
        _decayFactor = decayFactor;
        _bucketWidth = (long)Math.Ceiling(1d / error);
    }

    /// <summary>
    /// Gets how many observations the sketch has seen. One event is one observation, however many itemsets it
    /// contributed.
    /// </summary>
    public long Observed { get; private set; }

    /// <summary>
    /// Gets how many itemsets the sketch currently retains.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Gets the entries the sketch currently retains.
    /// </summary>
    public IEnumerable<LossyCountingEntry> Entries => _entries.Values.Select(entry => entry.ToEntry());

    long CurrentBucket => (long)Math.Ceiling(Observed * _error);

    /// <summary>
    /// Count one observation and the itemsets it contributed.
    /// </summary>
    /// <param name="itemsets">The candidate <see cref="FacetSet">itemsets</see> the observation contributed.</param>
    /// <param name="occurred"><see cref="DateTimeOffset">When</see> the observation occurred.</param>
    /// <remarks>
    /// The number of observations grows by one, not by the number of itemsets. Support is a share of events, so
    /// counting each of an event's itemsets as an observation would divide every frequency by a denominator many
    /// times too large and no pattern would ever clear the threshold.
    /// </remarks>
    public void Observe(IEnumerable<FacetSet> itemsets, DateTimeOffset occurred)
    {
        Observed++;
        var bucket = CurrentBucket;

        foreach (var itemset in itemsets)
        {
            if (_entries.TryGetValue(itemset.Key, out var existing))
            {
                existing.Count(occurred, _decayFactor);
            }
            else
            {
                _entries[itemset.Key] = new MutableEntry(itemset, bucket - 1, occurred);
            }
        }

        if (Observed % _bucketWidth == 0)
        {
            Prune();
        }
    }

    /// <summary>
    /// Seed the sketch with entries counted by an earlier life of it, continuing from where that one left off.
    /// </summary>
    /// <param name="entries">The <see cref="LossyCountingEntry">entries</see> to seed with.</param>
    /// <param name="observed">How many observations the earlier life had seen.</param>
    /// <remarks>
    /// The in-memory sketch dies with its process while what survived it is persisted, so a fresh sketch that
    /// starts from zero would report its first few observations with full support - and rewriting a scope from
    /// that would wipe established behavior in favor of whatever happened right after a restart. Restoring puts
    /// the persisted survivors back with the counts they had, so mining continues instead of starting over.
    /// Restored entries carry no error term: they were surviving patterns, counted precisely for as long as they
    /// have been retained.
    /// </remarks>
    public void Restore(IEnumerable<LossyCountingEntry> entries, long observed)
    {
        Observed = observed;

        foreach (var entry in entries)
        {
            _entries[entry.Itemset.Key] = MutableEntry.From(entry);
        }
    }

    /// <summary>
    /// Drop every itemset that has not kept up with the stream.
    /// </summary>
    /// <remarks>
    /// Called automatically at each bucket boundary, and exposed so an owner can prune on its own cadence - a
    /// low-throughput scope may take a long time to fill a bucket, and its dormant itemsets should still decay out
    /// rather than sit there forever.
    /// </remarks>
    public void Prune()
    {
        var bucket = CurrentBucket;
        var stale = _entries
            .Where(pair => pair.Value.Frequency + pair.Value.Error <= bucket)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in stale)
        {
            _entries.Remove(key);
        }
    }

    /// <summary>
    /// Apply decay to every itemset as of a point in time, without counting an observation.
    /// </summary>
    /// <param name="asOf"><see cref="DateTimeOffset">When</see> to decay as of.</param>
    /// <remarks>
    /// Decay is otherwise only applied when an itemset is counted again, so a scope that has gone quiet would keep
    /// the weight it had on its last good day forever. This is the background pass that lets dormant behavior fade.
    /// </remarks>
    public void Decay(DateTimeOffset asOf)
    {
        foreach (var entry in _entries.Values)
        {
            entry.DecayTo(asOf, _decayFactor);
        }
    }

    /// <summary>
    /// Try to get the entry for a specific itemset.
    /// </summary>
    /// <param name="key">The <see cref="FacetSetKey"/> to get for.</param>
    /// <param name="entry">The <see cref="LossyCountingEntry"/> when found.</param>
    /// <returns>True when the sketch retains the itemset, false when not.</returns>
    public bool TryGet(FacetSetKey key, out LossyCountingEntry entry)
    {
        if (_entries.TryGetValue(key, out var existing))
        {
            entry = existing.ToEntry();
            return true;
        }

        entry = default!;
        return false;
    }

    /// <summary>
    /// Gets the frequency counted for a specific itemset.
    /// </summary>
    /// <param name="key">The <see cref="FacetSetKey"/> to get for.</param>
    /// <returns>The counted frequency, or zero when the sketch does not retain the itemset.</returns>
    public long GetFrequency(FacetSetKey key) =>
        _entries.TryGetValue(key, out var existing) ? existing.Frequency : 0L;

    /// <summary>
    /// Represents the mutable state the sketch keeps per itemset.
    /// </summary>
    /// <param name="itemset">The <see cref="FacetSet"/> being counted.</param>
    /// <param name="error">The largest number of occurrences that could have been missed before it entered.</param>
    /// <param name="occurred">When the observation that created the entry occurred.</param>
    /// <remarks>
    /// <c>LastSeen</c> answers "when did this behavior last happen" and only ever moves on an observation. The
    /// moment the weight is decayed to is tracked separately, because it also moves on a background decay pass.
    /// Collapsing the two would make a decay pass look like an occurrence in the stored pattern, and would let two
    /// passes over the same interval decay it twice.
    /// </remarks>
    sealed class MutableEntry(FacetSet itemset, long error, DateTimeOffset occurred)
    {
        DateTimeOffset _weightAsOf = occurred;

        public long Frequency { get; private set; } = 1;

        public long Error { get; } = error;

        public double Weight { get; private set; } = 1d;

        public DateTimeOffset FirstSeen { get; private set; } = occurred;

        public DateTimeOffset LastSeen { get; private set; } = occurred;

        public static MutableEntry From(LossyCountingEntry entry) =>
            new(entry.Itemset, entry.Error, entry.FirstSeen)
            {
                Frequency = entry.Frequency,
                Weight = entry.Weight,
                LastSeen = entry.LastSeen,
                _weightAsOf = entry.LastSeen
            };

        public void Count(DateTimeOffset occurred, double decayFactor)
        {
            Frequency++;
            Weight = Decayed(occurred, decayFactor) + 1d;

            if (occurred > _weightAsOf)
            {
                _weightAsOf = occurred;
            }

            if (occurred < FirstSeen)
            {
                FirstSeen = occurred;
            }

            if (occurred > LastSeen)
            {
                LastSeen = occurred;
            }
        }

        public void DecayTo(DateTimeOffset asOf, double decayFactor)
        {
            if (asOf <= _weightAsOf)
            {
                return;
            }

            Weight = Decayed(asOf, decayFactor);
            _weightAsOf = asOf;
        }

        public LossyCountingEntry ToEntry() => new(itemset, Frequency, Error, Weight, FirstSeen, LastSeen);

        double Decayed(DateTimeOffset asOf, double decayFactor)
        {
            var days = (asOf - _weightAsOf).TotalDays;
            return days <= 0d ? Weight : Weight * Math.Pow(decayFactor, days);
        }
    }
}
