// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_root_without_a_declared_key_is_loaded : given.a_first_level_child_future
{
    bool _resolvedAfterMongoReload;
    bool _childAfterMongoReload;
    bool _childAfterSqlReload;

    void Establish() => SetFutureKey("root-key");

    async Task Because()
    {
        await ProcessRoot(RootWith("_id", "root-key"));
        _resolvedAfterMongoReload = _resolved;
        _childAfterMongoReload = HasChild;

        _resolved = false;
        _tracker.HasPending = true;
        await ProcessRoot(RootWith());
        _childAfterSqlReload = HasChild;
    }

    [Fact] void should_resolve_after_a_mongo_reload() => _resolvedAfterMongoReload.ShouldBeTrue();
    [Fact] void should_project_the_child_after_a_mongo_reload() => _childAfterMongoReload.ShouldBeTrue();
    [Fact] void should_project_the_child_after_a_sql_reload() => _childAfterSqlReload.ShouldBeTrue();
}
