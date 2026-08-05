// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The message on a property-level <c>[Unique]</c>, which used to be dropped here and nowhere else - the
/// class-level provider reads the same argument off the same attribute.
/// </summary>
/// <remarks>
/// The discard was invisible because everything else about the path worked: the constraint registered, the append
/// was correctly rejected, and only the human-readable half was missing - replaced by the same empty default an
/// attribute with no message produces. An author who had just written a message and watched the rejection happen
/// had every reason to look for the loss in their own presentation layer.
/// <para>
/// Several properties can share one constraint name and a constraint carries one message, so the first supplied
/// wins - the same answer the fluent form gives when it merges same-named definitions and keeps the first
/// callback.
/// </para>
/// </remarks>
public class when_providing_with_a_message : Specification
{
    const string NamedConstraint = "NamedConstraint";
    const string GroupedConstraint = "GroupedConstraint";
    const string SilentConstraint = "SilentConstraint";
    const string Message = "That account number is already taken.";
    const string GroupMessage = "That pair is already taken.";

    IClientArtifactsProvider _clientArtifactsProvider;
    IEventTypes _eventTypes;
    UniqueConstraintProvider _provider;
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();

        Register<AccountOpened>();
        Register<FirstGrouped>();
        Register<SecondGrouped>();
        Register<WithoutMessage>();

        _clientArtifactsProvider.UniqueConstraints.Returns(
        [
            typeof(AccountOpened),
            typeof(FirstGrouped),
            typeof(SecondGrouped),
            typeof(WithoutMessage)
        ]);

        _provider = new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes, new DefaultNamingPolicy());
    }

    void Because() => _result = _provider.Provide();

    UniqueConstraintDefinition Named => _result.OfType<UniqueConstraintDefinition>().Single(_ => _.Name == (ConstraintName)NamedConstraint);
    UniqueConstraintDefinition Grouped => _result.OfType<UniqueConstraintDefinition>().Single(_ => _.Name == (ConstraintName)GroupedConstraint);
    UniqueConstraintDefinition Silent => _result.OfType<UniqueConstraintDefinition>().Single(_ => _.Name == (ConstraintName)SilentConstraint);

    [Fact] void should_carry_the_message_that_was_written() => Named.MessageCallback(null!).ShouldEqual((ConstraintViolationMessage)Message);
    [Fact] void should_keep_the_first_message_in_a_group() => Grouped.MessageCallback(null!).ShouldEqual((ConstraintViolationMessage)GroupMessage);
    [Fact] void should_leave_a_constraint_with_no_message_undefined() => Silent.MessageCallback(null!).ShouldEqual(ConstraintViolationMessage.NotDefined);

    void Register<T>()
    {
        var eventType = new EventType(typeof(T).Name, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(T)).Returns(eventType);
        _eventTypes.GetSchemaFor(eventType.Id).Returns(JsonSchema.FromType<T>());
    }

    record AccountOpened([property: Unique(NamedConstraint, Message)] string AccountNumber);
    record FirstGrouped([property: Unique(GroupedConstraint, GroupMessage)] string First);
    record SecondGrouped([property: Unique(GroupedConstraint, "a later one")] string Second);
    record WithoutMessage([property: Unique(SilentConstraint)] string Value);
}
