// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.given;

public class a_fenced_batch_executor : Specification
{
    protected static readonly FenceToken _token = new("store/namespace/model", 1, 7);
    protected IFenceStore _store;
    protected List<IFenceTransaction> _transactions;
    protected FencedBatchExecutor _executor;
    protected int _writesCalled;
    protected Func<Task<string>> _writes;

    protected static MongoException Labeled(string label)
    {
        var error = new MongoException(label);
        error.AddErrorLabel(label);
        return error;
    }

    protected IFenceTransaction Transaction(int attempt) => _transactions[attempt];

    void Establish()
    {
        _store = Substitute.For<IFenceStore>();
        _transactions = [];
        _store.Begin(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var transaction = Substitute.For<IFenceTransaction>();
            transaction.Admit(Arg.Any<FenceToken>(), Arg.Any<FenceBatchId>(), Arg.Any<CancellationToken>()).Returns(true);
            _transactions.Add(transaction);
            ConfigureTransaction(transaction, _transactions.Count - 1);
            return transaction;
        });
        _writes = () => Task.FromResult("written");
        _executor = new(_store, new FenceRetryPolicy(5, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(30)), NullFencedBatchProbe.Instance);
    }

    protected virtual void ConfigureTransaction(IFenceTransaction transaction, int attempt)
    {
    }

    protected Task<string> Execute() => _executor.Execute(_token, _ =>
    {
        _writesCalled++;
        return _writes();
    });
}
