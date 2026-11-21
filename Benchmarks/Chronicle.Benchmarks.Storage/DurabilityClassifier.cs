// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Reduces a provider's durability settings to a class. Two results are only a ranking when their classes are equal;
/// otherwise the providers made different tradeoffs and the harness says so instead of ranking them.
/// </summary>
public static class DurabilityClassifier
{
    /// <summary>A commit is flushed to stable storage before it is acknowledged.</summary>
    public const string SyncOnCommit = "sync-on-commit";

    /// <summary>A commit is acknowledged from a log that is flushed on an interval or at checkpoints, so a crash can lose it.</summary>
    public const string DeferredFlush = "deferred-flush";

    /// <summary>A commit is acknowledged without any flush.</summary>
    public const string NoFlush = "no-flush";

    /// <summary>The settings could not be read.</summary>
    public const string Unknown = "unknown";

    /// <summary>Classifies SQLite from `PRAGMA synchronous` and `PRAGMA journal_mode`.</summary>
    /// <param name="synchronous">The synchronous setting: 0 OFF, 1 NORMAL, 2 FULL, 3 EXTRA.</param>
    /// <param name="journalMode">The journal mode.</param>
    /// <returns>The class.</returns>
    public static string ForSqlite(int synchronous, string journalMode) => synchronous switch
    {
        0 => NoFlush,
        1 when string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase) => DeferredFlush,
        1 => SyncOnCommit,
        2 or 3 => SyncOnCommit,
        _ => Unknown
    };

    /// <summary>Classifies PostgreSQL from `synchronous_commit` and `fsync`.</summary>
    /// <param name="synchronousCommit">The `synchronous_commit` setting.</param>
    /// <param name="fsync">The `fsync` setting.</param>
    /// <returns>The class.</returns>
    public static string ForPostgreSql(string synchronousCommit, string fsync)
    {
        if (string.Equals(fsync, "off", StringComparison.OrdinalIgnoreCase)) return NoFlush;
        return string.Equals(synchronousCommit, "off", StringComparison.OrdinalIgnoreCase) ? DeferredFlush : SyncOnCommit;
    }

    /// <summary>Classifies SQL Server from the database's delayed durability setting.</summary>
    /// <param name="delayedDurability">The `delayed_durability_desc` value: `DISABLED`, `ALLOWED` or `FORCED`.</param>
    /// <returns>The class.</returns>
    public static string ForSqlServer(string delayedDurability) =>
        string.Equals(delayedDurability, "FORCED", StringComparison.OrdinalIgnoreCase) ? DeferredFlush : SyncOnCommit;

    /// <summary>Classifies MongoDB from the effective write concern.</summary>
    /// <param name="writeConcern">The effective `w`: a number, `majority`, or <see langword="null"/> when the server decides.</param>
    /// <param name="journal">The effective `j`, or <see langword="null"/> when the server decides.</param>
    /// <param name="majorityJournalDefault">The replica set's `writeConcernMajorityJournalDefault`.</param>
    /// <returns>The class.</returns>
    public static string ForMongo(string? writeConcern, bool? journal, bool majorityJournalDefault)
    {
        var majority = writeConcern is null || string.Equals(writeConcern, "majority", StringComparison.OrdinalIgnoreCase);
        if (journal == true) return SyncOnCommit;
        if (journal == false) return DeferredFlush;
        if (majority && majorityJournalDefault) return SyncOnCommit;
        return writeConcern == "0" ? NoFlush : DeferredFlush;
    }

    /// <summary>Decides whether a set of results is comparable.</summary>
    /// <param name="classes">The durability class of each provider's result.</param>
    /// <returns><see langword="true"/> when there are at least two results and all share one known class; such results may be ranked. Otherwise they report a different tradeoff.</returns>
    public static bool AreComparable(IEnumerable<string> classes)
    {
        var distinct = classes.Distinct(StringComparer.Ordinal).ToArray();
        return distinct.Length == 1 && distinct[0] != Unknown && classes.Count() >= 2;
    }

    /// <summary>Gets the word to put beside a comparison row.</summary>
    /// <param name="classes">The durability class of each provider's result.</param>
    /// <returns>`comparable` or `different tradeoff`.</returns>
    public static string ComparabilityLabel(IEnumerable<string> classes) => AreComparable(classes) ? "comparable" : "different tradeoff";
}
