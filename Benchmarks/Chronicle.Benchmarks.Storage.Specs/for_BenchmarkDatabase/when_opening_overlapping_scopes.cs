// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkDatabase;

/// <summary>Verifies options reuse and independent context ownership across overlapping scopes.</summary>
public class when_opening_overlapping_scopes : Specification
{
    SqlProviderHost _host = null!;
    bool _sameOptions;
    bool _distinctContexts;
    bool _distinctScopes;
    int _count;
    Exception? _disposed;

    async Task Establish()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
        _host.CreateSequence();
    }

    async Task Because()
    {
        await using var first = await _host.Database.Namespace("test-store", "test-namespace");
        await using var second = await _host.Database.Namespace("test-store", "test-namespace");
        _sameOptions = ReferenceEquals(first.DbContext.GetService<IDbContextOptions>(), second.DbContext.GetService<IDbContextOptions>());
        _distinctContexts = !ReferenceEquals(first.DbContext, second.DbContext);
        _distinctScopes = !ReferenceEquals(first, second);
        await first.DisposeAsync();
        _disposed = Catch.Exception(() => _ = first.DbContext.EventSequences.ToArray());
        _count = await second.DbContext.EventSequences.CountAsync();
    }

    async Task Destroy() => await _host.DisposeAsync();

    [Fact] void should_reuse_options() => _sameOptions.ShouldBeTrue();
    [Fact] void should_create_fresh_contexts() => _distinctContexts.ShouldBeTrue();
    [Fact] void should_create_fresh_scopes() => _distinctScopes.ShouldBeTrue();
    [Fact] void should_dispose_the_owned_context() => _disposed.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_leave_the_other_context_usable() => _count.ShouldEqual(0);
}
