// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence.given;

/// <summary>
/// A read model with data in its primary table next to a table that is not one of its occurrences. A read naming an
/// occurrence the read model never had must neither touch that table nor create one.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISqlSinkHarness"/> supplying the provider under specification.</typeparam>
public abstract class a_read_model_with_a_canary<THarness> : for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected const string Canary = "canary";
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);
    static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(300);

    protected ReadModelInstances? _instances;
    protected bool _received;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 1UL);
        await CreateCanary();
    }

    /// <summary>
    /// Reads an occurrence through the sink the way the read boundary does, keeping the page it returned.
    /// </summary>
    /// <param name="occurrence">The occurrence named by the caller.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// A failure is not caught: every occurrence read here is answered with a page, and one that is not fails the specification.
    /// </remarks>
    protected async Task ReadOccurrence(string occurrence)
    {
        _instances = await _sink.GetInstances(occurrence, 0, 10, ReadModelInstanceVisibility.InitializedOnly);
        _received = true;
    }

    /// <summary>
    /// Observes an occurrence through the sink the way the read boundary does, keeping the first page it delivered.
    /// </summary>
    /// <param name="occurrence">The occurrence named by the caller.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// A failure or a missing page is not caught: the wait times out and the specification fails, so storage is only
    /// inspected once the query has answered. The subscription is ended and any poll still in flight is awaited before the specification inspects or tears down storage.
    /// </remarks>
    protected async Task ObserveOccurrence(string occurrence)
    {
        var first = new TaskCompletionSource<IEnumerable<ExpandoObject>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (_sink.ObserveInstances(occurrence, 0, 10, ReadModelInstanceVisibility.InitializedOnly)
            .Subscribe(page => first.TrySetResult(page), error => first.TrySetException(error)))
        {
            var page = (await first.Task.WaitAsync(_deadline)).ToArray();
            _instances = new ReadModelInstances(page, page.Length);
            _received = true;
        }

        // Unsubscribing stops further polls but does not wait for one already running, which could still be
        // creating or reading a table while the specification inspects or drops storage.
        await _sqlHarness.Reads.WhenQuiet(_settle, _deadline);
    }

    /// <summary>
    /// Creates a table straight through the provider, bypassing the sink.
    /// </summary>
    /// <param name="table">The name of the table to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task CreateTable(string table)
    {
        await using var context = OpenInspectionContext();
        var helper = context.GetService<ISqlGenerationHelper>();
#pragma warning disable CA2100, EF1002 // Specification-owned identifiers delimited by the provider.
        await context.Database.ExecuteSqlRawAsync($"CREATE TABLE {helper.DelimitIdentifier(table)} ({helper.DelimitIdentifier("Id")} integer NOT NULL)");
#pragma warning restore CA2100, EF1002
    }

    /// <summary>
    /// Gets whether the table that is not an occurrence of the read model is still there.
    /// </summary>
    /// <returns>True if it exists; false otherwise.</returns>
    protected Task<bool> CanaryExists() => TableExists(Canary);

    /// <summary>
    /// Gets the number of instances the read returned, zero when it failed.
    /// </summary>
    /// <returns>The number of instances.</returns>
    protected int InstanceCount() => _instances?.Instances.Count() ?? 0;

    /// <summary>
    /// Gets whether the read delivered a page, which is what makes inspecting storage afterwards meaningful.
    /// </summary>
    /// <returns>True if a page, possibly empty, was received.</returns>
    protected bool PageReceived() => _received;

    Task CreateCanary() => CreateTable(Canary);
}
