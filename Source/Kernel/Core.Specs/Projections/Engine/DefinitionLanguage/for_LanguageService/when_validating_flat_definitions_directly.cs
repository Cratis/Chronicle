// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_validating_flat_definitions_directly : given.a_runtime_language_service
{
    [Fact]
    void should_accept_a_complete_scalar_root_tuple() => FlatCompositeKeyDefinition.Validate(Definition(), _readModel);

    [Fact]
    void should_refuse_all_event_subscriptions() =>
        Catch.Exception(() => FlatCompositeKeyDefinition.Validate(Definition() with { SubscribesToAllEvents = true }, _readModel)).ShouldBeOfExactType<FlatCompositeKeyNotSupported>();

    [Fact]
    void should_refuse_an_incomplete_removal_tuple()
    {
        var definition = Definition() with
        {
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>
            {
                [(EventType)"Removed"] = new(WellKnownExpressions.EventSourceId, null)
            }
        };
        Catch.Exception(() => FlatCompositeKeyDefinition.Validate(definition, _readModel)).ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
    }

    [Fact]
    void should_refuse_unknown_root_properties()
    {
        var definition = Definition() with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [(EventType)"Changed"] = new(new Dictionary<PropertyPath, string>(), "$flatComposite(name=$eventContent.name,unknown=$eventContent.age)", null)
            }
        };
        Catch.Exception(() => FlatCompositeKeyDefinition.Validate(definition, _readModel)).ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("Id")]
    void should_explain_why_an_id_part_is_refused(string part)
    {
        var definition = Definition();
        definition = definition with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [(EventType)"Changed"] = definition.From[(EventType)"Changed"] with
                {
                    Key = $"$flatComposite({part}=$eventContent.name,age=$eventContent.age)"
                }
            }
        };
        var exception = Catch.Exception(() => FlatCompositeKeyDefinition.Validate(definition, _readModel));
        exception.ShouldBeOfExactType<InvalidFlatCompositeKeyExpression>();
        exception!.Message.ShouldContain($"part '{part}' is not supported as a flat composite key part");
    }

    static ProjectionDefinition Definition() => global::Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("projection", "Alert") with
    {
        SubscribesToAllEvents = false,
        From = new Dictionary<EventType, FromDefinition>
        {
            [(EventType)"Changed"] = new(new Dictionary<PropertyPath, string>(), "$flatComposite(name=$eventContent.name,age=$eventContent.age)", null)
        }
    };
}
