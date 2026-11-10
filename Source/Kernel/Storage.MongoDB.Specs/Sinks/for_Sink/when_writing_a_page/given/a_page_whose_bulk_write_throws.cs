// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page.given;

/// <summary>
/// A page sink whose bulk write throws something other than a per-document write failure.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the database.</param>
public abstract class a_page_whose_bulk_write_throws(MongoDBFixture fixture) : a_page_sink(fixture)
{
    protected FailedPartition[] _failures;

    /// <summary>
    /// Gets the <see cref="Exception"/> the bulk write throws.
    /// </summary>
    protected abstract Exception Failure { get; }

    protected FailedPartition FailureFor(string partition) => _failures.Single(_ => _.EventSourceId.Value.Equals(partition));

    protected Task<IEnumerable<FailedPartition>> WriteThePage() => Pages.ApplyPage([Write("first", 1), Write("second", 2), Write("third", 3)]);

    void Establish() => _bulkWriteFailure = Failure;
}
