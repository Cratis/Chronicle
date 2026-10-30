// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

public class and_an_older_insert_arrives_after_the_update : Contract.an_isolated_read_model<SqlSinkHarness>
{
    readonly inserting_writer _writer = new();
    SqlSinkHarness _harness;
    Task _insert = Task.CompletedTask;
    IsolatedReplaceResult _result;
    int _count;

    protected override SqlSinkHarness CreateHarness() => _harness = new() { Interceptors = [_writer] };

    void Establish()
    {
        var live = _harness.CreateSinkForTheSameReadModel();
        _writer.Insert = () => _insert = Task.Run(() => live.ApplyChanges(_key, ChangesetSettingCountTo(10), 10, SinkWriteMode.GuardedUpsert));
    }

    async Task Because()
    {
        _result = await _sink.ReplaceIsolated(_key, State(20), 20);
        await _insert;
        _count = await CurrentCount();
    }

    [Fact] void should_not_discard_the_newer_replacement() => _result.ShouldEqual(IsolatedReplaceResult.Replaced);
    [Fact] void should_keep_the_newer_state() => _count.ShouldEqual(20);
    [Fact] void should_serialize_the_existence_check_with_the_update() => _writer.Isolation.ShouldEqual(IsolationLevel.Serializable);

    sealed class inserting_writer : DbCommandInterceptor
    {
        public Func<Task>? Insert { get; set; }
        public IsolationLevel? Isolation { get; private set; }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (Insert is not { } insert || result != 0)
            {
                return result;
            }
            if (!command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal))
            {
                return result;
            }
            Insert = null;
            Isolation = command.Transaction?.IsolationLevel;
            var pending = insert();

            // Without a transaction the old row can land between UPDATE and Any. With a serializable
            // writer it must wait for the replacement to commit, so never await it while holding the lock.
            if (command.Transaction is null)
            {
                await pending.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
