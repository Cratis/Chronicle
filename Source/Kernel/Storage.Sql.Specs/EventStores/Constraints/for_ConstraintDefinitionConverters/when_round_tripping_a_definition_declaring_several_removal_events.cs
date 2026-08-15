// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_ConstraintDefinitionConverters;

/// <summary>
/// The SQL provider persists a definition as JSON and reads it back through the same serializer, so a definition
/// that survives the trip is the whole of what registration compares against. Every removal event has to come back,
/// not just the first.
/// </summary>
/// <remarks>
/// It also guards a hazard the compatibility overloads introduce. System.Text.Json refuses a type with more than one
/// public constructor unless one is marked, and both definition records now have two — the plural primary and the
/// obsolete single-removal shim. Nothing else in the suite reads a constraint definition back out of JSON, so
/// without this the SQL provider would throw on the first definition it read and no spec would say why.
/// </remarks>
public class when_round_tripping_a_definition_declaring_several_removal_events : Specification
{
    static readonly ConstraintName _name = "unique-invited-address";
    static readonly EventTypeId _acceptedEventTypeId = "InvitationAccepted";
    static readonly EventTypeId _revokedEventTypeId = "InvitationRevoked";

    UniqueConstraintDefinition _unique;
    UniqueEventTypeConstraintDefinition _uniqueEventType;
    IConstraintDefinition _uniqueResult;
    IConstraintDefinition _uniqueEventTypeResult;

    void Establish()
    {
        _unique = new(_name, [new("InvitationSent", ["EmailAddress"])], [_acceptedEventTypeId, _revokedEventTypeId], true);
        _uniqueEventType = new("loan-open", [(EventTypeId)"LoanCheckedOut"], [_acceptedEventTypeId, _revokedEventTypeId]);
    }

    void Because()
    {
        _uniqueResult = _unique.ToSql(1).ToKernel();
        _uniqueEventTypeResult = _uniqueEventType.ToSql(1).ToKernel();
    }

    [Fact] void should_read_back_the_unique_constraint_that_was_written() => _uniqueResult.ShouldEqual(_unique);
    [Fact] void should_keep_every_removal_event_of_the_unique_constraint() => ((UniqueConstraintDefinition)_uniqueResult).RemovedWith.ShouldContainOnly([_acceptedEventTypeId, _revokedEventTypeId]);
    [Fact] void should_read_back_the_unique_event_type_constraint_that_was_written() => _uniqueEventTypeResult.ShouldEqual(_uniqueEventType);
    [Fact] void should_keep_every_removal_event_of_the_unique_event_type_constraint() => ((UniqueEventTypeConstraintDefinition)_uniqueEventTypeResult).RemovedWith.ShouldContainOnly([_acceptedEventTypeId, _revokedEventTypeId]);
}
