// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Stops fixture resets after the first confirmed container exit.
/// </summary>
/// <param name="containerId">The container identity to report.</param>
/// <param name="inspect">Refreshes state from Docker; null means running or unknown.</param>
/// <param name="logs">Reads a bounded diagnostic tail.</param>
/// <param name="delay">Waits between state probes and retries.</param>
internal sealed class KernelFailureGuard(
    string containerId,
    Func<CancellationToken, Task<long?>> inspect,
    Func<CancellationToken, Task<string>> logs,
    Func<CancellationToken, Task> delay)
{
    readonly object _gate = new();
    Task<ExceptionDispatchInfo>? _failure;

    /// <summary>
    /// Rethrows the fixture's retained first cause, without another reset or probe.
    /// </summary>
    /// <returns>The retained failure, or a completed task when no exit has been confirmed.</returns>
    internal Task ThrowIfFailed()
    {
        lock (_gate)
        {
            return _failure is null ? Task.CompletedTask : Rethrow(_failure);
        }
    }

    /// <summary>
    /// Checks fresh container state and shares the first confirmed exit with all callers.
    /// </summary>
    /// <param name="cancellationToken">Cancels the state inspection.</param>
    /// <returns>The check task.</returns>
    internal async Task Check(CancellationToken cancellationToken)
    {
        Task<ExceptionDispatchInfo>? failure;
        lock (_gate)
        {
            failure = _failure;
        }
        if (failure is not null)
        {
            (await failure).Throw();
        }

        long? exitCode;
        try
        {
            exitCode = await inspect(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // An unavailable Docker endpoint is not evidence of container exit.
            return;
        }
        if (exitCode is not null)
        {
            (await Capture(exitCode.Value)).Throw();
        }
        await ThrowIfFailed();
    }

    /// <summary>
    /// Runs reset attempts while monitoring the kernel, retaining the existing retry deadline.
    /// </summary>
    /// <param name="reset">The cancellable reset operation.</param>
    /// <param name="canRetry">Whether the existing deadline permits another attempt.</param>
    /// <param name="retry">The retry wait, including optional timing instrumentation.</param>
    /// <returns>The reset task.</returns>
    internal async Task Run(Func<CancellationToken, Task> reset, Func<bool> canRetry, Func<Task>? retry = null)
    {
        await Check(CancellationToken.None);
        while (true)
        {
            try
            {
                await RunAttempt(reset);
                return;
            }
            catch (Exception exception) when (exception is not KernelProcessExited)
            {
                await Check(CancellationToken.None);
                if (!canRetry())
                {
                    throw;
                }
                await (retry?.Invoke() ?? delay(CancellationToken.None));
                await Check(CancellationToken.None);
            }
        }
    }

    static async Task Rethrow(Task<ExceptionDispatchInfo> failure) => (await failure).Throw();

    static async Task InvokeReset(Func<CancellationToken, Task> reset, CancellationToken cancellationToken) => await reset(cancellationToken);

    static async Task Observe(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // Cancellation callbacks and both losers are joined, without replacing the winning cause.
            return;
        }
    }

    async Task RunAttempt(Func<CancellationToken, Task> reset)
    {
        using var cancellation = new CancellationTokenSource();
        Task operation;
        Task<ExceptionDispatchInfo>? failure;
        lock (_gate)
        {
            // Admission is atomic with the first-exit latch, even between the precheck and RPC start.
            failure = _failure;
            operation = failure is null ? InvokeReset(reset, cancellation.Token) : Task.CompletedTask;
        }
        if (failure is not null)
        {
            (await failure).Throw();
        }
        var monitor = Monitor(cancellation.Token);
        try
        {
            await Task.WhenAny(operation, monitor).Unwrap();
            await Check(cancellation.Token);
        }
        finally
        {
            await Observe(cancellation.CancelAsync());
            await Observe(operation);
            await Observe(monitor);
        }
    }

    async Task Monitor(CancellationToken cancellationToken)
    {
        while (true)
        {
            await delay(cancellationToken);
            await Check(cancellationToken);
        }
    }

    Task<ExceptionDispatchInfo> Capture(long exitCode)
    {
        TaskCompletionSource<ExceptionDispatchInfo>? capture = null;
        Task<ExceptionDispatchInfo> failure;
        lock (_gate)
        {
            if (_failure is null)
            {
                capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _failure = capture.Task;
            }
            failure = _failure;
        }
        if (capture is not null)
        {
            _ = CaptureDiagnostics(exitCode, capture);
        }
        return failure;
    }

    async Task CaptureDiagnostics(long exitCode, TaskCompletionSource<ExceptionDispatchInfo> capture)
    {
        string tail;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            tail = await logs(cancellation.Token);
        }
        catch (Exception)
        {
            // Diagnostics are secondary; never include a transport error that may contain credentials.
            tail = "[diagnostics unavailable]";
        }
        capture.SetResult(ExceptionDispatchInfo.Capture(new KernelProcessExited(containerId, exitCode, tail)));
    }
}
