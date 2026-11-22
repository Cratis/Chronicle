// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

[PerFactSpecificationLifecycle]
public class when_running_v2_transactions : given.a_serverless_v2_storage
{
    IMongoClient _client = null!;
    IClientSessionHandle _session = null!;
    V2Transactions _transactions = null!;

    void Establish()
    {
        _client = Substitute.For<IMongoClient>();
        _session = Substitute.For<IClientSessionHandle>();
        _database.Client.Returns(_client);
        _client.StartSessionAsync(cancellationToken: Arg.Any<CancellationToken>()).Returns(Task.FromResult(_session));
        _session.IsInTransaction.Returns(true);
        _session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _session.AbortTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _transactions = new(_database, ReadConcern.Snapshot);
    }

    [Fact]
    public async Task should_finish_commit_without_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        _session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        });
        (await _transactions.Run(_ => Task.FromResult((42, true)), cancellation.Token)).ShouldEqual(42);
        await _session.Received(1).CommitTransactionAsync(CancellationToken.None);
    }

    [Fact]
    public async Task should_abort_cancellation_before_commit_starts()
    {
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _transactions.Run(
            _ =>
            {
                cancellation.Cancel();
                return Task.FromResult((42, true));
            },
            cancellation.Token));
        await _session.Received(1).AbortTransactionAsync(CancellationToken.None);
        await _session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_abort_a_refusal_without_committing_its_fence()
    {
        (await _transactions.Run(_ => Task.FromResult((false, false)), CancellationToken.None)).ShouldBeFalse();
        await _session.Received(1).AbortTransactionAsync(CancellationToken.None);
        await _session.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_retry_the_same_commit_without_replaying_the_mutation()
    {
        var unknown = new MongoException("ambiguous");
        unknown.AddErrorLabel("UnknownTransactionCommitResult");
        _session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(unknown), Task.CompletedTask);
        var mutations = 0;
        (await _transactions.Run(_ => Task.FromResult((++mutations, true)), CancellationToken.None)).ShouldEqual(1);
        mutations.ShouldEqual(1);
        await _client.Received(1).StartSessionAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _session.Received(2).CommitTransactionAsync(CancellationToken.None);
    }

    [Fact]
    public async Task should_retry_a_definite_abort_with_a_fresh_attempt()
    {
        var transient = new MongoException("aborted");
        transient.AddErrorLabel("TransientTransactionError");
        var attempts = 0;
        (await _transactions.Run(
            async _ =>
            {
                await Task.CompletedTask;
                if (++attempts == 1) throw transient;
                return (attempts, true);
            },
            CancellationToken.None)).ShouldEqual(2);
        await _client.Received(2).StartSessionAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _session.Received(1).AbortTransactionAsync(CancellationToken.None);
    }
}
