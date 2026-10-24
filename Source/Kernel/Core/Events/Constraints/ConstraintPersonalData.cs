// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Describes which parts of an event are personal data, so a unique-index claim derived from them can record its subject.
/// </summary>
public static class ConstraintPersonalData
{
    static readonly ConditionalWeakTable<JsonSchema, IReadOnlyCollection<string>> _properties = [];

    /// <summary>
    /// Gets the top-level properties that carry personal data, anywhere beneath them, according to the schema.
    /// </summary>
    /// <param name="schema">The event type's schema generation.</param>
    /// <returns>The property names.</returns>
    public static IReadOnlyCollection<string> PropertiesOf(JsonSchema schema) =>
        _properties.GetValue(schema, Resolve);

    /// <summary>
    /// Establishes the validation context of a stored event, carrying the subject of its personal data.
    /// </summary>
    /// <param name="event">The stored event.</param>
    /// <param name="content">The event's released content.</param>
    /// <param name="personalProperties">The event's top-level properties that carry personal data.</param>
    /// <param name="validators">The validators of the context.</param>
    /// <returns>The <see cref="ConstraintValidationContext"/>.</returns>
    public static ConstraintValidationContext ContextFor(AppendedEvent @event, System.Dynamic.ExpandoObject content, IReadOnlyCollection<string> personalProperties, IEnumerable<IConstraintValidator> validators) =>
        new(validators, @event.Context.EventSourceId, @event.Context.EventType.Id, content, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId)
        {
            Subject = SubjectOf(@event.Context),
            PersonalProperties = personalProperties
        };

    /// <summary>
    /// Gets the subject an event's personal data belongs to.
    /// </summary>
    /// <param name="context">The event's context.</param>
    /// <returns>The explicit subject, or the event source id when none is set.</returns>
    public static Subject SubjectOf(EventContext context) =>
        context.Subject?.IsSet == true ? context.Subject : new Subject(context.EventSourceId.Value);

    /// <summary>
    /// Gets the subject a claim must record: only when a property the constraint covers for the event type is personal data.
    /// </summary>
    /// <param name="definition">The unique constraint.</param>
    /// <param name="context">The validation context.</param>
    /// <returns>The subject, or null when the claim holds no personal data.</returns>
    public static Subject? SubjectFor(this UniqueConstraintDefinition definition, ConstraintValidationContext context)
    {
        if (context.Subject is null || context.PersonalProperties.Count == 0) return null;
        var eventDefinition = definition.EventDefinitions.SingleOrDefault(_ => _.EventTypeId == context.EventTypeId);
        if (eventDefinition is null) return null;

        // A nested path is personal when its top-level property carries personal data anywhere beneath it. This can
        // record a subject for a non-personal sibling, which only means erasure frees it too: the safe direction.
        return eventDefinition.Properties.Any(property => context.PersonalProperties.Contains(property.Split('.')[0], StringComparer.OrdinalIgnoreCase))
            ? context.Subject
            : null;
    }

    static IReadOnlyCollection<string> Resolve(JsonSchema schema) =>
        schema.ActualSchema.ActualProperties
            .Where(property => property.Value.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == ComplianceMetadataType.PII.Value))
            .Select(property => property.Key)
            .ToArray();
}
