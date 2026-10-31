// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceDbContext;

public class when_mapping_closed_stream_scopes : given.a_migrated_namespace_database
{
    IEnumerable<string> _key;

    void Because()
    {
        using var context = CreateContext();
        _key = context.Model.FindEntityType(typeof(ClosedStreamEntry))!.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray();
    }

    [Fact] void should_key_by_sequence_and_exact_owned_scope() => _key.ShouldContainOnly("EventSequenceId", "StreamType", "StreamId", "EventSourceId", "EventSourceType", "Owner", "Dimensions");
}
