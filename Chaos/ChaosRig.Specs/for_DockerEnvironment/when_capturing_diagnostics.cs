// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DockerEnvironment;

/// <summary>
/// Specifies that diagnostic capture cannot replace the original run failure.
/// </summary>
public class when_capturing_diagnostics : Specification
{
    bool _captured;

    async Task Because() => await DockerEnvironment.CaptureLogsFor("test", () =>
    {
        _captured = true;
        return Task.CompletedTask;
    });

    [Fact] void should_attempt_capture() => _captured.ShouldBeTrue();
    [Fact] async Task should_tolerate_an_uncreated_container() => await DockerEnvironment.CaptureLogsFor("uncreated", () => throw new InvalidOperationException("Resource not created."));
    [Fact] async Task should_tolerate_capture_timeout() => await DockerEnvironment.CaptureLogsFor("timed-out", () => Task.FromCanceled(new CancellationToken(canceled: true)));
}
