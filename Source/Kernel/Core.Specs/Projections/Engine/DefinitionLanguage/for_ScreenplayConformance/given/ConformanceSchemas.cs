// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Semantics;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The schemas Chronicle's engine would receive for a case, taken from the event and read-model declarations of the case's
/// <c language="csharp">.play</c> application. Chronicle resolves AutoMap at runtime against these schemas, so they are what lets its
/// effective mappings be compared with the ones the semantic model expands at bind time.
/// </summary>
/// <param name="events">The schema per event name.</param>
/// <param name="root">The read-model level of the projection.</param>
public class ConformanceSchemas(IReadOnlyDictionary<string, JsonSchema> events, ConformanceLevel root)
{
    /// <summary>
    /// Gets the read-model level of the projection.
    /// </summary>
    public ConformanceLevel Root => root;

    /// <summary>
    /// Creates the schemas for a compiled case.
    /// </summary>
    /// <param name="application">The compiled application.</param>
    /// <param name="slice">The slice holding the case's declarations.</param>
    /// <param name="projection">The projection under comparison.</param>
    /// <returns>The schemas.</returns>
    public static ConformanceSchemas For(SemanticApplication application, SemanticSlice slice, SemanticProjection projection)
    {
        var types = application.Types.ToDictionary(_ => _.Id, _ => _.Properties.AsEnumerable());
        var events = slice.Events.ToDictionary(_ => _.Name, _ => Schema(_.Properties), StringComparer.Ordinal);
        var readModel = slice.ReadModels.Single(_ => _.Id == projection.ReadModel);
        return new(events, Level(readModel.Properties, types));
    }

    /// <summary>
    /// Gets the schema of an event.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <returns>The schema, or null when the case declares no such event.</returns>
    public JsonSchema? Event(string name) => events.TryGetValue(name, out var schema) ? schema : null;

    static ConformanceLevel Level(IEnumerable<SemanticProperty> properties, IReadOnlyDictionary<SemanticId, IEnumerable<SemanticProperty>> types)
    {
        var children = new Dictionary<string, Func<ConformanceLevel>>(StringComparer.Ordinal);
        foreach (var property in properties.Where(_ => _.Type.Kind == SemanticTypeReferenceKind.CompositeType && types.ContainsKey(_.Type.Target)))
        {
            children[property.Name] = () => Level(types[property.Type.Target], types);
        }

        return new(Schema(properties), property => children.TryGetValue(property, out var level) ? level() : null);
    }

    static JsonSchema Schema(IEnumerable<SemanticProperty> properties)
    {
        // Chronicle's AutoMap matches property names only, so the property types carry no meaning here.
        var node = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() };
        foreach (var property in properties)
        {
            node["properties"]![property.Name] = new JsonObject { ["type"] = property.Type.IsCollection ? "array" : "string" };
        }

        return new JsonSchema(node);
    }
}
