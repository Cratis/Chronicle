// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ContentRootLocator;

public class when_finding_the_content_root_from_many_threads_at_once : Specification
{
    const int Callers = 64;
    ContentRoot[] _results;

    async Task Because()
    {
        using var start = new Barrier(Callers);
        var assembly = typeof(when_finding_the_content_root_from_many_threads_at_once).Assembly;
        _results = await Task.WhenAll(Enumerable.Range(0, Callers).Select(_ => Task.Factory.StartNew(
            () =>
            {
                start.SignalAndWait();
                return ContentRootLocator.FindFor(assembly);
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default)));
    }

    [Fact] void should_give_every_caller_the_same_directory() => _results.Select(_ => _.Value).Distinct().Count().ShouldEqual(1);
    [Fact] void should_give_a_directory_that_holds_a_project_file() => Directory.EnumerateFiles(_results[0], "*.csproj").ShouldNotBeEmpty();
}
