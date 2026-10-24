// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_output_is_unavailable : Specification
{
    readonly Exception _original = new InvalidOperationException("Original failure");
    Exception? _caught;

    async Task Because()
    {
        var timings = new IntegrationTimings(_ => throw new IOException(), () => 10, 1000, "fixture");
        _caught = await Catch.Exception(() => timings.Run("scenario", () => Task.FromException(_original)));
        timings.Complete();
    }

    [Fact] void should_preserve_the_action_failure() => _caught.ShouldBeSame(_original);
    [Fact] void should_restore_the_ambient_scope() => IntegrationTimings.CurrentPhase.ShouldBeNull();
}
