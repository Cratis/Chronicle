// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Records every command and transaction an EF context runs, and lets a specification arrange a one-shot fault
/// or an interleaving at an exact command or commit.
/// </summary>
/// <remarks>
/// The hooks are one-shot and use <see cref="Interlocked"/> flags, because a hook may drive a competing sink whose
/// contexts carry this same interceptor.
/// </remarks>
public class SqlCommandCapture : IDbCommandInterceptor, IDbTransactionInterceptor
{
    readonly ConcurrentQueue<string> _commands = new();
    int _started;
    int _committed;
    int _rolledBack;
    int _lost;
    int _savepoints;
    Hook? _beforeCommand;
    Hook? _afterCommand;
    CommitHook? _afterCommit;
    Func<DbConnection, Task>? _afterTransactionStarted;

    /// <summary>
    /// Gets the text of every command, in the order they started.
    /// </summary>
    public IReadOnlyList<string> Commands => _commands.ToArray();

    /// <summary>
    /// Gets the number of transactions started.
    /// </summary>
    public int Started => _started;

    /// <summary>
    /// Gets the number of transactions committed.
    /// </summary>
    public int Committed => _committed;

    /// <summary>
    /// Gets the number of transactions rolled back.
    /// </summary>
    public int RolledBack => _rolledBack;

    /// <summary>
    /// Gets the number of commit acknowledgments lost.
    /// </summary>
    public int LostAcknowledgments => _lost;

    /// <summary>
    /// Gets the number of savepoints created.
    /// </summary>
    public int Savepoints => _savepoints;

    /// <summary>
    /// Gets a stable hash of the command texts, in order.
    /// </summary>
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', _commands)))).ToLowerInvariant();

    /// <summary>
    /// Gets the commands that modify data, which are the INSERT, UPDATE and DELETE statements.
    /// </summary>
    public IReadOnlyList<string> WriteCommands => Commands.Where(IsWrite).ToArray();

    /// <summary>
    /// Determines whether the command text modifies data.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <returns>True when it inserts, updates or deletes.</returns>
    public static bool IsWrite(string commandText) =>
        commandText.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase) ||
        commandText.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase) ||
        commandText.Contains("DELETE FROM", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Forgets everything recorded so far. Armed hooks stay armed.
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
        _started = 0;
        _committed = 0;
        _rolledBack = 0;
        _lost = 0;
        _savepoints = 0;
    }

    /// <summary>
    /// Runs an action once, before the first command matching the predicate executes.
    /// </summary>
    /// <param name="match">Selects the command by its text.</param>
    /// <param name="action">The action, which may throw to fail the command.</param>
    public void BeforeCommand(Func<string, bool> match, Func<DbCommand, Task> action) => _beforeCommand = new(match, action);

    /// <summary>
    /// Runs an action once, after the first command matching the predicate has executed.
    /// </summary>
    /// <param name="match">Selects the command by its text.</param>
    /// <param name="action">The action.</param>
    public void AfterCommand(Func<string, bool> match, Func<DbCommand, Task> action) => _afterCommand = new(match, action);

    /// <summary>
    /// Throws once, before the first command matching the predicate executes.
    /// </summary>
    /// <param name="match">Selects the command by its text.</param>
    /// <param name="exception">The exception to throw.</param>
    public void ThrowBeforeCommand(Func<string, bool> match, Exception exception) => BeforeCommand(match, _ => throw exception);

    /// <summary>
    /// Runs an action once, after the first commit that happens from now on, and then optionally reports the
    /// commit as lost by throwing, the way a dropped acknowledgment looks to the caller.
    /// </summary>
    /// <param name="action">The action, which runs after the server has committed.</param>
    /// <param name="loseAcknowledgment">Whether to throw a <see cref="TimeoutException"/> afterwards.</param>
    public void AfterNextCommit(Func<Task> action, bool loseAcknowledgment) => _afterCommit = new(action, loseAcknowledgment);

    /// <summary>
    /// Throws a <see cref="TimeoutException"/> once, before the next commit is sent, so the server never commits.
    /// </summary>
    public void FailNextCommitBeforeSending() => AfterNextCommit(() => Task.CompletedTask, false, beforeSending: true);

    /// <summary>
    /// Runs an action once, right after the next transaction has started.
    /// </summary>
    /// <param name="action">The action, given the connection the transaction started on.</param>
    public void AfterNextTransactionStarted(Func<DbConnection, Task> action) => _afterTransactionStarted = action;

    /// <inheritdoc/>
    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
        Executing(command, result);

    /// <inheritdoc/>
    public ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        Executing(command, result);

    /// <inheritdoc/>
    public ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default) =>
        Executing(command, result);

    /// <inheritdoc/>
    public async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        await Executed(command);
        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await Executed(command);
        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        await Executed(command);
        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _started);
        var hook = Interlocked.Exchange(ref _afterTransactionStarted, null);
        if (hook is not null)
        {
            await hook(connection);
        }

        return result;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        var failing = _afterCommit;
        if (failing?.BeforeSending == true && ReferenceEquals(Interlocked.CompareExchange(ref _afterCommit, null, failing), failing))
        {
            throw new TimeoutException("Simulated failure before the commit was sent");
        }

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _committed);
        var hook = Interlocked.Exchange(ref _afterCommit, null);
        if (hook is null)
        {
            return;
        }

        await hook.Action();
        if (hook.LoseAcknowledgment)
        {
            Interlocked.Increment(ref _lost);
            throw new TimeoutException("Simulated lost commit acknowledgment");
        }
    }

    /// <inheritdoc/>
    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _rolledBack);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult> CreatingSavepointAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _savepoints);
        return ValueTask.FromResult(result);
    }

    void AfterNextCommit(Func<Task> action, bool loseAcknowledgment, bool beforeSending) => _afterCommit = new(action, loseAcknowledgment) { BeforeSending = beforeSending };

    async ValueTask<InterceptionResult<T>> Executing<T>(DbCommand command, InterceptionResult<T> result)
    {
        _commands.Enqueue(command.CommandText);
        var hook = _beforeCommand;
        if (hook?.Match(command.CommandText) == true && ReferenceEquals(Interlocked.CompareExchange(ref _beforeCommand, null, hook), hook))
        {
            await hook.Action(command);
        }

        return result;
    }

    async Task Executed(DbCommand command)
    {
        var hook = _afterCommand;
        if (hook?.Match(command.CommandText) == true && ReferenceEquals(Interlocked.CompareExchange(ref _afterCommand, null, hook), hook))
        {
            await hook.Action(command);
        }
    }

    sealed record Hook(Func<string, bool> Match, Func<DbCommand, Task> Action);

    sealed record CommitHook(Func<Task> Action, bool LoseAcknowledgment)
    {
        public bool BeforeSending { get; init; }
    }
}

