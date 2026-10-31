// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionRegistrationSignature;

/// <summary>
/// Two registrations that differ in anything a definition says must not be taken for one another. Every property of
/// a definition is listed here with a way to change it, and a property added later fails the completeness
/// specification until it is listed too - so the signature can never silently stop covering part of a definition.
/// </summary>
public class when_a_definition_property_differs : a_registration
{
    /// <summary>
    /// The properties that are deliberately not part of the signature, with the reason.
    /// </summary>
    static readonly Dictionary<string, string> _excluded = new()
    {
        [nameof(ProjectionDefinition.LastUpdated)] = "A timestamp the receiver controls; it is not something the client registered.",
        [nameof(ProjectionDefinition.IsKernelOwned)] = "Derived from Owner, which is covered.",
        [nameof(ProjectionDefinition.IsEmpty)] = "Derived from From, Children and Nested, which are covered.",
        ["EqualityContract"] = "Compiler generated."
    };

    static readonly Dictionary<string, Func<ProjectionDefinition, ProjectionDefinition>> _changes = new()
    {
        [nameof(ProjectionDefinition.Owner)] = _ => _ with { Owner = ProjectionOwner.Kernel },
        [nameof(ProjectionDefinition.EventSequenceId)] = _ => _ with { EventSequenceId = new EventSequenceId("another") },
        [nameof(ProjectionDefinition.Identifier)] = _ => _ with { Identifier = new ProjectionId("another") },
        [nameof(ProjectionDefinition.ReadModel)] = _ => _ with { ReadModel = "another" },
        [nameof(ProjectionDefinition.IsActive)] = _ => _ with { IsActive = !_.IsActive },
        [nameof(ProjectionDefinition.IsRewindable)] = _ => _ with { IsRewindable = !_.IsRewindable },
        [nameof(ProjectionDefinition.InitialModelState)] = _ => _ with { InitialModelState = new JsonObject { ["count"] = 1 } },
        [nameof(ProjectionDefinition.From)] = _ => _ with { From = new Dictionary<EventType, FromDefinition> { [FirstEvent] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"name"] = "changed" }, PropertyExpression.NotSet, null) } },
        [nameof(ProjectionDefinition.Join)] = _ => _ with { Join = new Dictionary<EventType, JoinDefinition> { [FirstEvent] = new((PropertyPath)"on", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, AutoMap.Inherit) } },
        [nameof(ProjectionDefinition.Children)] = _ => _ with { Children = new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"items"] = Child() } },
        [nameof(ProjectionDefinition.FromDerivatives)] = _ => _ with { FromDerivatives = [new FromDerivatives([FirstEvent], new FromDefinition(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null))] },
        [nameof(ProjectionDefinition.FromEvery)] = _ => _ with { FromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"all"] = "all" }, false) },
        [nameof(ProjectionDefinition.RemovedWith)] = _ => _ with { RemovedWith = new Dictionary<EventType, RemovedWithDefinition> { [FirstEvent] = new(PropertyExpression.NotSet, null) } },
        [nameof(ProjectionDefinition.RemovedWithJoin)] = _ => _ with { RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition> { [FirstEvent] = new(PropertyExpression.NotSet) } },
        [nameof(ProjectionDefinition.FromEventProperty)] = _ => _ with { FromEventProperty = new FromEventPropertyDefinition(FirstEvent, PropertyExpression.NotSet) },
        [nameof(ProjectionDefinition.Tags)] = _ => _ with { Tags = ["a-tag"] },
        [nameof(ProjectionDefinition.AutoMap)] = _ => _ with { AutoMap = AutoMap.Disabled },
        [nameof(ProjectionDefinition.Nested)] = _ => _ with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"inner"] = Child() } },
        [nameof(ProjectionDefinition.SubscribesToAllEvents)] = _ => _ with { SubscribesToAllEvents = true },
        [nameof(ProjectionDefinition.NoAutoMapProperties)] = _ => _ with { NoAutoMapProperties = [(PropertyPath)"skipped"] },
        [nameof(ProjectionDefinition.Scope)] = _ => _ with { Scope = ProjectionScope.Global },
        [nameof(ProjectionDefinition.GenerationDelivery)] = _ => _ with { GenerationDelivery = Concepts.Observation.EventGenerationDelivery.Pinned },
        [nameof(ProjectionDefinition.Version)] = _ => _ with { Version = "9.9.9" },
        [nameof(ProjectionDefinition.HasLegacyStorageFields)] = _ => _ with { HasLegacyStorageFields = !_.HasLegacyStorageFields }
    };

    string[] _unlistedProperties;
    string[] _unchangingProperties;

    void Because()
    {
        var properties = typeof(ProjectionDefinition)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        _unlistedProperties = [.. properties.Where(name => !_changes.ContainsKey(name) && !_excluded.ContainsKey(name))];

        var original = SignatureOf(Definition());
        _unchangingProperties = [.. _changes
            .Where(change => properties.Contains(change.Key) && SignatureOf(change.Value(Definition())) == original)
            .Select(change => change.Key)];
    }

    [Fact] void should_list_every_property_of_a_definition() => _unlistedProperties.ShouldBeEmpty();
    [Fact] void should_change_the_signature_for_every_listed_property() => _unchangingProperties.ShouldBeEmpty();

    static ChildrenDefinition Child() => new(
        (PropertyPath)"id",
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
