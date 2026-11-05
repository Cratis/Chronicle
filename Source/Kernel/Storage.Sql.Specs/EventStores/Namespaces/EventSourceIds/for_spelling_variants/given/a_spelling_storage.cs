// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.given;

public abstract class a_spelling_storage(SpellingHarness harness) : Specification
{
    protected readonly SpellingHarness _harness = harness;

    public static SpellingHarness Sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-spelling-{Guid.NewGuid():N}.db");
        return new("sqlite", () => Task.FromResult($"DataSource={path};Pooling=False;Default Timeout=30"), _ =>
        {
            File.Delete(path);
            return Task.CompletedTask;
        });
    }

    async Task Establish() => await _harness.Initialize();
    async Task Destroy() => await _harness.DisposeAsync();
}
