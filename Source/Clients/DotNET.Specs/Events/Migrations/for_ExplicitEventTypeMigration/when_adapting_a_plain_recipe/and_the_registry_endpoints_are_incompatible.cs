// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_the_registry_endpoints_are_incompatible : given.a_plain_migration
{
    IEventTypes _differentIds;
    IEventTypes _gap;
    IEventTypes _missing;
    Exception _idError;
    Exception _gapError;
    Exception _missingError;

    void Establish()
    {
        _differentIds = Substitute.For<IEventTypes>();
        _differentIds.GetEventTypeFor(typeof(Previous)).Returns(new EventType("old", 1));
        _differentIds.GetEventTypeFor(typeof(Upgrade)).Returns(new EventType("new", 2));
        _gap = Substitute.For<IEventTypes>();
        _gap.GetEventTypeFor(typeof(Previous)).Returns(new EventType("shared", 1));
        _gap.GetEventTypeFor(typeof(Upgrade)).Returns(new EventType("shared", 3));
        _missing = Substitute.For<IEventTypes>();
        _missing.GetEventTypeFor(typeof(Previous)).Throws(new TypeIsNotAnEventType(typeof(Previous)));
    }

    void Because()
    {
        _idError = Catch.Exception(() => _methods.Create(_services, _differentIds));
        _gapError = Catch.Exception(() => _methods.Create(_services, _gap));
        _missingError = Catch.Exception(() => _methods.Create(_services, _missing));
    }

    [Fact] void should_reject_different_registered_identifiers_without_reflection_wrapping() => _idError.ShouldBeOfExactType<MigrationGenerationsMustShareEventTypeId>();
    [Fact] void should_reject_nonadjacent_registered_generations_without_reflection_wrapping() => _gapError.ShouldBeOfExactType<InvalidMigrationGenerationGap>();
    [Fact] void should_require_both_registered_endpoints() => _missingError.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
