// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_saving_a_definition_to_history : given.a_history_provider
{
    Task Because() => Write();
    [Fact]
    void should_record_history_before_saving_active() => Received.InOrder(() =>
    {
        _ = _history.Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
        _ = _eventStoreStorage.Projections.Save(_firstDefinition);
    });
    [Fact] Task should_record_the_definition_and_metadata() => _history.Received(1).Record(Arg.Is<ProjectionDefinitionHistoryEntry>(_ => _.Projection == _firstDefinition.Identifier && _.Definition == _firstDefinition && _.RegisteringVersion == _firstDefinition.Version && _.Digest == ProjectionFactory.DefinitionDigestFor(_firstDefinition) && _.DigestAlgorithm == ProjectionFactory.DefinitionDigestAlgorithm && _.RecordedAt == _now));
}
