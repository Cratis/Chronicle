// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_jitter_is_selected : Specification
{
    [Theory]
    [InlineData(0, 25)]
    [InlineData(25, 25)]
    [InlineData(63, 63)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    async Task should_stay_in_the_bounded_range(int sample, int expected)
    {
        var delays = new List<TimeSpan>();
        var budget = new NamespaceCommitRetries(
            TimeProvider.System,
            (duration, _) =>
            {
                delays.Add(duration);
                return Task.CompletedTask;
            },
            () => sample);
        using var lifetime = budget.Bind(new(Concepts.NamespaceIncarnationId.New(), CancellationToken.None));
        await budget.BeginAttempt(lifetime);
        await budget.BeginAttempt(lifetime);
        delays.Single().ShouldEqual(TimeSpan.FromMilliseconds(expected));
    }
}
