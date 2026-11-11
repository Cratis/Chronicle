// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares a <see langword="lock"/> on an <see cref="object"/> with one on <see cref="System.Threading.Lock"/>, uncontended and
/// with four threads. Run with --filter '*LockBenchmark*' --runtimes net10.0 net11.0 --memory.
/// </summary>
[MemoryDiagnoser]
public class LockBenchmark
{
    const int Iterations = 1_000;
    const int Threads = 4;
    readonly object _objectLock = new();
    readonly Lock _lock = new();
    int _counter;

    /// <summary>
    /// Takes the object lock on a single thread.
    /// </summary>
    /// <returns>The counter.</returns>
    [Benchmark(Baseline = true)]
    public int ObjectUncontended()
    {
        for (var i = 0; i < Iterations; i++)
        {
            lock (_objectLock)
            {
                _counter++;
            }
        }

        return _counter;
    }

    /// <summary>
    /// Takes the <see cref="System.Threading.Lock"/> on a single thread.
    /// </summary>
    /// <returns>The counter.</returns>
    [Benchmark]
    public int LockUncontended()
    {
        for (var i = 0; i < Iterations; i++)
        {
            lock (_lock)
            {
                _counter++;
            }
        }

        return _counter;
    }

    /// <summary>
    /// Takes the object lock from four threads.
    /// </summary>
    /// <returns>The counter.</returns>
    [Benchmark]
    public int ObjectContended()
    {
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, _ =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                lock (_objectLock)
                {
                    _counter++;
                }
            }
        });

        return _counter;
    }

    /// <summary>
    /// Takes the <see cref="System.Threading.Lock"/> from four threads.
    /// </summary>
    /// <returns>The counter.</returns>
    [Benchmark]
    public int LockContended()
    {
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, _ =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                lock (_lock)
                {
                    _counter++;
                }
            }
        });

        return _counter;
    }
}
