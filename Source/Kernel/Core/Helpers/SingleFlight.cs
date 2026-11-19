// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Helpers;

/// <summary>
/// Lets callers that ask the same question at the same moment share one answer.
/// </summary>
/// <typeparam name="TKey">The type of key identifying a question.</typeparam>
/// <typeparam name="TResult">The type of answer.</typeparam>
/// <remarks>
/// <para>
/// Nothing is cached. An answer is shared only with callers that arrive while the lookup that produces it is still
/// running; a caller arriving after it completed starts a new lookup. A shared answer is therefore never older than the
/// time one lookup takes, which is what makes this safe for the periodic safety-net checks that must see current state.
/// </para>
/// <para>
/// A failed lookup fails every caller that shared it, a cancelled one cancels every caller that shared it, and the
/// next caller starts a new one.
/// </para>
/// </remarks>
internal sealed class SingleFlight<TKey, TResult>
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, Task<TResult>> _inFlight = new();

    /// <summary>
    /// Gets the number of lookups currently running.
    /// </summary>
    internal int InFlight => _inFlight.Count;

    /// <summary>
    /// Runs a lookup, or joins the one that is already running for the same key.
    /// </summary>
    /// <param name="key">The key identifying the question.</param>
    /// <param name="lookup">The lookup to run when none is running for the key.</param>
    /// <returns>The answer of the shared lookup.</returns>
    public Task<TResult> Run(TKey key, Func<Task<TResult>> lookup)
    {
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = _inFlight.GetOrAdd(key, completion.Task);
        return ReferenceEquals(current, completion.Task) ? Lead(key, completion, lookup) : current;
    }

    async Task<TResult> Lead(TKey key, TaskCompletionSource<TResult> completion, Func<Task<TResult>> lookup)
    {
        try
        {
            var result = await lookup().ConfigureAwait(false);
            _inFlight.TryRemove(new KeyValuePair<TKey, Task<TResult>>(key, completion.Task));
            completion.SetResult(result);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task<TResult>>(key, completion.Task));

            // Joined callers observe the cancellation the same way the leader does.
            completion.SetCanceled(ex.CancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task<TResult>>(key, completion.Task));
            completion.SetException(ex);

            // The leader rethrows below; mark the shared task observed so it is not reported when nobody joined.
            _ = completion.Task.Exception;
            throw;
        }
    }
}
