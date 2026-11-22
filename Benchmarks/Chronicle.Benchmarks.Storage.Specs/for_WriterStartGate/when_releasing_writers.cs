// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_WriterStartGate;

public class when_releasing_writers : Specification
{
    readonly WriterStartGate _gate = new();
    Task<long>[] _writers = [];
    long[] _started = [];
    bool _allWaiting;

    void Establish()
    {
        _writers = Enumerable.Range(0, 4).Select(async _ =>
        {
            await _gate.Wait();
            return Stopwatch.GetTimestamp();
        }).ToArray();
        _allWaiting = _writers.All(_ => !_.IsCompleted);
    }

    async Task Because()
    {
        _gate.Release();
        _started = await Task.WhenAll(_writers).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_hold_every_timed_section_until_release() => _allWaiting.ShouldBeTrue();
    [Fact] void should_release_every_writer() => _started.Length.ShouldEqual(4);
    [Fact] void should_start_each_timed_section_after_release() => _started.ShouldEachConformTo(_ => _ >= _gate.ReleasedAt);
}
