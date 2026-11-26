// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Ensures that unsupported asynchronous boundaries cannot silently produce undercounts.</summary>
public class when_sampling_incomplete_work : Specification
{
    Exception? _incomplete;
    Exception? _faulted;
    Exception? _nonGeneric;

    void Because()
    {
        var pending = new TaskCompletionSource<int>();
        _incomplete = Catch.Exception(() => _ = AllocationProbe.Bytes(() => _ = AllocationProbe.Complete(pending.Task)));
        _faulted = Catch.Exception(() => _ = AllocationProbe.Complete(Task.FromException<int>(new InvalidOperationException("fixture failure"))));
        _nonGeneric = Catch.Exception(() => AllocationProbe.Complete((Task)pending.Task));
    }

    [Fact] void should_reject_incomplete_generic_tasks() => _incomplete.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_faulted_tasks() => _faulted.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_incomplete_non_generic_tasks() => _nonGeneric.ShouldBeOfExactType<InvalidOperationException>();
}
