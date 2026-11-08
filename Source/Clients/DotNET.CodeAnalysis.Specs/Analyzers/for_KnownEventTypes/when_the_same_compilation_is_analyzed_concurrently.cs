// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KnownEventTypes;

public class when_the_same_compilation_is_analyzed_concurrently : Specification
{
    Compilation _compilation;
    ImmutableArray<ITypeSymbol>[] _eventTypes;

    async Task Establish() => _compilation = (await TestProject.CreateProject(BatchObserverSource.Create("public class Consumer {}")).GetCompilationAsync())!;

    async Task Because() => _eventTypes = await Task.WhenAll(Enumerable.Range(0, 32)
        .Select(_ => Task.Run(() => KnownEventTypes.For(_compilation, CancellationToken.None))));

    [Fact] void should_find_the_event_types() => _eventTypes[0].IsEmpty.ShouldBeFalse();
    [Fact] void should_share_one_collected_array() => _eventTypes.All(eventTypes => eventTypes == _eventTypes[0]).ShouldBeTrue();
    [Fact] void should_not_share_them_with_a_different_compilation() =>
        (KnownEventTypes.For(_compilation.Clone(), CancellationToken.None) == _eventTypes[0]).ShouldBeFalse();
}
