// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

public partial class InMemoryGraphSink
{
    void Apply(GraphOperation operation, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        switch (operation)
        {
            case UpsertNode upsert:
                UpsertNodeElement(upsert, owner, sequence, journal);
                break;
            case SetProperties set:
                SetPropertiesOnElement(set, owner, sequence, journal);
                break;
            case DeleteNode delete:
                DeleteNodeElement(delete, owner, sequence, journal);
                break;
            case UpsertEdge upsert:
                UpsertEdgeElement(upsert, owner, sequence, journal);
                break;
            case DeleteEdge delete:
                DeleteEdgeElement(delete, owner, sequence, journal);
                break;
            default:
                throw new NotSupportedException($"The graph operation '{operation.GetType().Name}' is not supported.");
        }
    }

    void UpsertNodeElement(UpsertNode upsert, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        definition.GetNode(upsert.Node.Label);
        var id = InMemoryGraphNodeId.From(upsert.Node);
        var element = LiveNode(id, owner);
        if (element is null)
        {
            element = CreateNode(id, journal);
        }
        else if (IsAtOrBelowWatermark(element, sequence))
        {
            return;
        }

        Write(element, upsert.Properties, owner, sequence, journal);
    }

    void SetPropertiesOnElement(SetProperties set, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        var element = set.Element switch
        {
            NodeReference node => LiveNode(InMemoryGraphNodeId.From(node), owner),
            EdgeReference edge => LiveEdge(InMemoryGraphEdgeId.From(edge), owner),
            _ => null
        };

        if (element is null || IsAtOrBelowWatermark(element, sequence))
        {
            return;
        }

        Write(element, set.Properties, owner, sequence, journal);
    }

    void DeleteNodeElement(DeleteNode delete, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        definition.GetNode(delete.Node.Label);
        var id = InMemoryGraphNodeId.From(delete.Node);
        var element = LiveNode(id, owner);
        if (element is null || IsAtOrBelowWatermark(element, sequence))
        {
            return;
        }

        var attached = _edges.Keys.Where(edge => edge.Source == id || edge.Target == id).ToArray();
        if (attached.Length > 0 && !delete.Detach)
        {
            throw new NodeStillHasEdges(delete.Node);
        }

        foreach (var edge in attached) RemoveEdge(edge, journal);
        RemoveNode(id, journal);
    }

    void UpsertEdgeElement(UpsertEdge upsert, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        var edgeDefinition = definition.GetEdge(upsert.Edge.Type);
        if ((edgeDefinition.IdentifyingProperty is null) != (upsert.Edge.Identity is null))
        {
            throw new InvalidOperationException($"The edge type '{edgeDefinition.Type}' {(edgeDefinition.IdentifyingProperty is null ? "does not declare" : "requires")} an identifying property.");
        }

        if (upsert.Edge.Source.Label != edgeDefinition.SourceLabel || upsert.Edge.Target.Label != edgeDefinition.TargetLabel)
        {
            throw new InvalidOperationException($"The edge type '{edgeDefinition.Type}' connects '{edgeDefinition.SourceLabel}' to '{edgeDefinition.TargetLabel}'.");
        }

        var id = InMemoryGraphEdgeId.From(upsert.Edge);
        var element = LiveEdge(id, owner);
        if (element is not null && IsAtOrBelowWatermark(element, sequence))
        {
            return;
        }

        EnsureEndpoint(upsert.Edge.Source, owner, journal);
        EnsureEndpoint(upsert.Edge.Target, owner, journal);
        element ??= CreateEdge(id, journal);
        Write(element, upsert.Properties, owner, sequence, journal);
    }

    void DeleteEdgeElement(DeleteEdge delete, ProjectionId owner, ulong sequence, List<Action> journal)
    {
        definition.GetEdge(delete.Edge.Type);
        var id = InMemoryGraphEdgeId.From(delete.Edge);
        var element = LiveEdge(id, owner);
        if (element is null || IsAtOrBelowWatermark(element, sequence))
        {
            return;
        }

        RemoveEdge(id, journal);
    }
}
