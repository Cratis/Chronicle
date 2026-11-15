// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// The stable reason codes of the rules a graph view declaration must keep.
/// </summary>
public static class GraphViewReason
{
    /// <summary>
    /// The root of a view is not a node declared in the graph.
    /// </summary>
    public const string ViewRootNotANode = nameof(ViewRootNotANode);

    /// <summary>
    /// Two views have the same name.
    /// </summary>
    public const string DuplicateViewName = nameof(DuplicateViewName);

    /// <summary>
    /// A property member names a property the root node does not declare.
    /// </summary>
    public const string UnknownNodeProperty = nameof(UnknownNodeProperty);

    /// <summary>
    /// A relation names an edge type the graph does not declare.
    /// </summary>
    public const string UnknownEdge = nameof(UnknownEdge);

    /// <summary>
    /// An edge property member names a property the edge does not declare.
    /// </summary>
    public const string UnknownEdgeProperty = nameof(UnknownEdgeProperty);

    /// <summary>
    /// The edge of a relation is not incident on the root of the view.
    /// </summary>
    public const string EdgeNotIncidentOnRoot = nameof(EdgeNotIncidentOnRoot);

    /// <summary>
    /// The edge of a relation is a self edge and the relation gives no direction.
    /// </summary>
    public const string AmbiguousDirection = nameof(AmbiguousDirection);

    /// <summary>
    /// The root of the target view is not the node at the far end of the edge.
    /// </summary>
    public const string TargetLabelMismatch = nameof(TargetLabelMismatch);

    /// <summary>
    /// A relation leads to a view that is not declared.
    /// </summary>
    public const string RelationTargetNotAView = nameof(RelationTargetNotAView);

    /// <summary>
    /// A view contains itself, directly or through other views.
    /// </summary>
    public const string RecursiveView = nameof(RecursiveView);

    /// <summary>
    /// A view with edge property members is not the target of any relation.
    /// </summary>
    public const string EdgePropertyViewNotUsedAsTarget = nameof(EdgePropertyViewNotUsedAsTarget);

    /// <summary>
    /// A view with edge property members is the target of relations of more than one edge type.
    /// </summary>
    public const string EdgePropertyViewSharedAcrossEdges = nameof(EdgePropertyViewSharedAcrossEdges);

    /// <summary>
    /// A member is malformed: it lacks what its kind needs, or is a duplicate of another member of the view.
    /// </summary>
    public const string InvalidMember = nameof(InvalidMember);

    /// <summary>
    /// A query names a view or a member that does not exist, or a member of the wrong kind.
    /// </summary>
    public const string InvalidQuery = nameof(InvalidQuery);
}
