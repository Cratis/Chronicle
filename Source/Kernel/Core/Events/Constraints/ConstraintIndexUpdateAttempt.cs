// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Bounds and classifies one index write independently of its enclosing turn.
/// </summary>
internal static class ConstraintIndexUpdateAttempt
{
    /// <summary>
    /// Attempts a cancellable write for at most one second and classifies its outcome.
    /// </summary>
    /// <param name="update">The storage write.</param>
    /// <param name="cancellationToken">The enclosing deadline.</param>
    /// <param name="timeProvider">The clock for the individual attempt.</param>
    /// <returns>The outcome and its optional error.</returns>
    internal static async Task<(ConstraintIndexUpdateOutcome Outcome, Exception? Error)> Run(Func<CancellationToken, Task> update, CancellationToken cancellationToken, TimeProvider? timeProvider = null)
    {
        if (cancellationToken.IsCancellationRequested) return (ConstraintIndexUpdateOutcome.NotAttempted, null);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1), timeProvider ?? TimeProvider.System);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        Task? write = null;
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            write = update(deadline.Token);
            await write.WaitAsync(deadline.Token);
            return (ConstraintIndexUpdateOutcome.Applied, null);
        }
        catch (Exception error) when (deadline.IsCancellationRequested)
        {
            return (write is { IsCompleted: false } ? ConstraintIndexUpdateOutcome.Abandoned : ConstraintIndexUpdateOutcome.Failed, error);
        }
        catch (DuplicateUniqueConstraintValue error)
        {
            return (ConstraintIndexUpdateOutcome.Settled, error);
        }
        catch (Exception error) when (error is InvalidClosingStreamScope or RedactedClosingEventTypeUnavailable)
        {
            return (ConstraintIndexUpdateOutcome.SettledWithError, error);
        }
        catch (Exception error)
        {
            return (ConstraintIndexUpdateOutcome.Failed, error);
        }
    }
}
