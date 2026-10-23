// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents how fenced transactions retry transient transaction errors, such as a write conflict on the gate record.
/// </summary>
/// <param name="MaxAttempts">The maximum number of attempts.</param>
/// <param name="InitialBackoff">The backoff after the first transient failure; it doubles up to <paramref name="MaxBackoff"/>.</param>
/// <param name="MaxBackoff">The longest single backoff.</param>
/// <param name="Deadline">The longest total time spent retrying.</param>
/// <remarks>
/// The deadline bounds how long revocation or unknown-commit resolution waits for a batch the server still holds open; MongoDB aborts
/// such a transaction once its lifetime limit (transactionLifetimeLimitSeconds, 60 seconds by default) passes, so the default deadline is longer.
/// </remarks>
public record FenceRetryPolicy(int MaxAttempts, TimeSpan InitialBackoff, TimeSpan MaxBackoff, TimeSpan Deadline)
{
    /// <summary>
    /// The error label MongoDB attaches to an error that aborted a transaction which can be retried from the start.
    /// </summary>
    public const string TransientTransactionError = "TransientTransactionError";

    /// <summary>
    /// The error label MongoDB attaches to an error after which the commit of a transaction may or may not have happened.
    /// </summary>
    public const string UnknownTransactionCommitResult = "UnknownTransactionCommitResult";

    /// <summary>
    /// Gets the default <see cref="FenceRetryPolicy"/>.
    /// </summary>
    public static readonly FenceRetryPolicy Default = new(1000, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(90));

    /// <summary>
    /// Checks whether an error aborted a transaction that can be retried from the start.
    /// </summary>
    /// <param name="error">The error to check.</param>
    /// <returns>True if transient.</returns>
    public static bool IsTransient(Exception error) => error is MongoException mongo && mongo.HasErrorLabel(TransientTransactionError);

    /// <summary>
    /// Checks whether an error leaves the outcome of a commit unknown.
    /// </summary>
    /// <param name="error">The error to check.</param>
    /// <returns>True if the commit outcome is unknown.</returns>
    public static bool IsUnknownCommit(Exception error) => error is MongoException mongo && mongo.HasErrorLabel(UnknownTransactionCommitResult);

    /// <summary>
    /// Gets the backoff before a given retry.
    /// </summary>
    /// <param name="attempt">The one-based attempt that just failed.</param>
    /// <returns>The backoff.</returns>
    public TimeSpan BackoffFor(int attempt)
    {
        var factor = Math.Pow(2, Math.Min(attempt - 1, 16));
        var backoff = TimeSpan.FromTicks((long)Math.Min(InitialBackoff.Ticks * factor, MaxBackoff.Ticks));
        return backoff + TimeSpan.FromTicks(Random.Shared.NextInt64(Math.Max(1, backoff.Ticks / 2)));
    }

    /// <summary>
    /// Runs a whole transaction, starting it again after each transient transaction error within the policy's bounds.
    /// </summary>
    /// <typeparam name="T">The type of result.</typeparam>
    /// <param name="transaction">The transaction to run; each call must start its own session and transaction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The result of the attempt that committed.</returns>
    public async Task<T> RetryTransient<T>(Func<Task<T>> transaction, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await transaction();
            }
            catch (Exception error) when (IsTransient(error) && attempt < MaxAttempts && DateTimeOffset.UtcNow - started < Deadline)
            {
                await Task.Delay(BackoffFor(attempt), cancellationToken);
            }
        }
    }
}
