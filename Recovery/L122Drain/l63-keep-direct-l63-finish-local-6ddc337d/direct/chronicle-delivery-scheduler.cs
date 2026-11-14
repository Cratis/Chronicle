using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Runs deliveries for one observer: batches for the same partition run in arrival order, while batches for
/// different partitions run concurrently up to a bound. Barriers wait for everything in flight and hold back
/// everything scheduled after them.
/// </summary>
internal sealed class PartitionedDeliveryScheduler : IDisposable
{
	private const string SerialKey = "";

	private readonly SemaphoreSlim _slots;

	private readonly Dictionary<string, Task> _tails = new Dictionary<string, Task>();

	private readonly Lock _lock = new Lock();

	private Task _barrier = Task.CompletedTask;

	private volatile bool _stopped;

	/// <summary>
	/// Gets the default bound: the processor count, capped at eight.
	/// </summary>
	public static int DefaultMaxConcurrentPartitions => Math.Min(Environment.ProcessorCount, 8);

	/// <summary>
	/// Gets the maximum number of partitions handled at the same time.
	/// </summary>
	public int MaxConcurrentPartitions { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.Observation.PartitionedDeliveryScheduler" /> class.
	/// </summary>
	/// <param name="maxConcurrentPartitions">The maximum number of partitions handled at the same time. One is serial.</param>
	public PartitionedDeliveryScheduler(int maxConcurrentPartitions)
	{
		InvalidMaxConcurrentPartitions.ThrowIfInvalid(maxConcurrentPartitions);
		MaxConcurrentPartitions = maxConcurrentPartitions;
		_slots = new SemaphoreSlim(maxConcurrentPartitions, maxConcurrentPartitions);
	}

	/// <summary>
	/// Schedules work for a partition. It runs after earlier work for the same partition and after any barrier.
	/// </summary>
	/// <param name="partition">The partition the work belongs to.</param>
	/// <param name="work">The work to run.</param>
	/// <returns>A task that completes when the work has run or was skipped.</returns>
	public Task Schedule(string partition, Func<Task> work)
	{
		using (_lock.EnterScope())
		{
			string key = ((MaxConcurrentPartitions == 1) ? "" : partition);
			Task value;
			Task previous = (_tails.TryGetValue(key, out value) ? value : _barrier);
			Task task = StartDetached(() => Run(previous, work, useSlot: true));
			_tails[key] = task;
			task.ContinueWith(delegate
			{
				using (_lock.EnterScope())
				{
					if (_tails.TryGetValue(key, out Task value2) && value2 == task)
					{
						_tails.Remove(key);
					}
				}
			}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			return task;
		}
	}

	/// <summary>
	/// Schedules work that waits for all work in flight and holds back all work scheduled after it.
	/// </summary>
	/// <param name="work">The work to run.</param>
	/// <returns>A task that completes when the work has run or was skipped.</returns>
	public Task ScheduleBarrier(Func<Task> work)
	{
		using (_lock.EnterScope())
		{
			Task drain = Task.WhenAll(_tails.Values.Append(_barrier).ToArray());
			Task result = (_barrier = StartDetached(() => Run(drain, work, useSlot: false)));
			_tails.Clear();
			return result;
		}
	}

	/// <summary>
	/// Stops the scheduler. Work that has not started is skipped; running work finishes.
	/// </summary>
	public void Stop()
	{
		_stopped = true;
	}

	/// <summary>
	/// Waits for all work that is scheduled or running to finish. Failures are not rethrown: draining never throws.
	/// </summary>
	/// <returns>A task that completes when everything scheduled so far has finished.</returns>
	public async Task Drain()
	{
		Task[] array;
		using (_lock.EnterScope())
		{
			array = _tails.Values.Append(_barrier).ToArray();
		}
		Task[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			await array2[i].ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		}
	}

	/// <summary>
	/// Stops the scheduler, waits for running work to finish and releases its resources.
	/// </summary>
	/// <returns>A task that completes when the scheduler has drained and been disposed.</returns>
	public async Task DrainAndDispose()
	{
		Stop();
		await Drain();
		Dispose();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Stop();
		_slots.Dispose();
	}

	/// <summary>
	/// Starts work without the caller's execution context.
	/// </summary>
	/// <param name="work">The work to start.</param>
	/// <returns>The started work.</returns>
	/// <remarks>
	/// Work is scheduled from whatever context delivered the batch, such as a stream reader started inside an HTTP
	/// request that made the client connect. Handlers must never see that caller's ambient state, for example an
	/// HTTP context accessor pointing at a disposed request, so the work starts with a clean execution context.
	/// </remarks>
	private static Task StartDetached(Func<Task> work)
	{
		if (ExecutionContext.IsFlowSuppressed())
		{
			return Task.Run(work);
		}
		AsyncFlowControl asyncFlowControl = ExecutionContext.SuppressFlow();
		try
		{
			return Task.Run(work);
		}
		finally
		{
			asyncFlowControl.Undo();
		}
	}

	private async Task Run(Task previous, Func<Task> work, bool useSlot)
	{
		await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		if (_stopped)
		{
			return;
		}
		if (useSlot)
		{
			await _slots.WaitAsync();
		}
		try
		{
			if (!_stopped)
			{
				await work();
			}
		}
		catch
		{
			_stopped = true;
			throw;
		}
		finally
		{
			if (useSlot)
			{
				ReleaseSlot();
			}
		}
	}

	private void ReleaseSlot()
	{
		try
		{
			_slots.Release();
		}
		catch (ObjectDisposedException) when (_stopped)
		{
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
