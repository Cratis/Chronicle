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
    /// <returns>The outcome and its optional error.</returns>
    internal static async Task<(ConstraintIndexUpdateOutcome Outcome, Exception? Error)> Run(Func<CancellationToken, Task> update, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            await update(deadline.Token).WaitAsync(deadline.Token);
            return (ConstraintIndexUpdateOutcome.Applied, null);
        }
        catch (Exception error) when (cancellationToken.IsCancellationRequested)
        {
            return (ConstraintIndexUpdateOutcome.NotAttempted, error);
        }
        catch (Exception error) when (deadline.IsCancellationRequested)
        {
            return (ConstraintIndexUpdateOutcome.Abandoned, error);
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
