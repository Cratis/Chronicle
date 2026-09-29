// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_ConstraintDefinitionConverters;

/// <summary>
/// Every definition persisted before a constraint could declare its event sequences has no member for them in its
/// JSON. It must read back as applying to every event sequence, without a migration.
/// </summary>
public class when_reading_a_definition_persisted_before_it_could_declare_event_sequences : Specification
{
    ConstraintDefinition _entity;
    IConstraintDefinition _result;
    bool _currentShapeCarriesTheMember;

    void Establish()
    {
        // The shape the current kernel writes, less the member an earlier kernel did not have.
        _entity = new UniqueConstraintDefinition("unique-name", [new("NameClaimed", ["Name"])]).ToSql(1);
        var json = JsonNode.Parse(_entity.Definition)!.AsObject();
        _currentShapeCarriesTheMember = json.Remove(nameof(UniqueConstraintDefinition.EventSequences));
        _entity.Definition = json.ToJsonString();
    }

    void Because() => _result = _entity.ToKernel();

    [Fact] void should_have_removed_the_member_the_current_shape_carries() => _currentShapeCarriesTheMember.ShouldBeTrue();
    [Fact] void should_declare_no_event_sequences() => ((UniqueConstraintDefinition)_result).EventSequences.ShouldBeEmpty();
    [Fact] void should_apply_to_every_event_sequence() => _result.AppliesTo(EventSequenceId.Outbox).ShouldBeTrue();
}
