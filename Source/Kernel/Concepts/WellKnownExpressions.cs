// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Known expressions for projections.
/// </summary>
public static class WellKnownExpressions
{
    /// <summary>
    /// The event source identifier expression.
    /// </summary>
    public const string EventSourceId = "$eventSourceId";

    /// <summary>
    /// The add expression.
    /// </summary>
    public const string Add = "$add";

    /// <summary>
    /// The subtract expression.
    /// </summary>
    public const string Subtract = "$subtract";

    /// <summary>
    /// The increment expression.
    /// </summary>
    public const string Increment = "$increment";

    /// <summary>
    /// The decrement expression.
    /// </summary>
    public const string Decrement = "$decrement";

    /// <summary>
    /// The event context expression.
    /// </summary>
    public const string EventContext = "$eventContext";

    /// <summary>
    /// The event content expression, used as a dynamic dictionary key segment resolved from the event payload.
    /// </summary>
    public const string EventContent = "$eventContent";

    /// <summary>
    /// The set once expression - assigns the inner expression only when the target has no value.
    /// </summary>
    public const string SetOnce = "$setOnce";

    /// <summary>
    /// The remove expression - removes a dictionary key addressed by a dynamic key segment.
    /// </summary>
    public const string Remove = "$remove";

    /// <summary>
    /// The value expression.
    /// </summary>
    public const string Value = "$value";

    /// <summary>
    /// The count expression.
    /// </summary>
    public const string Count = "$count";

    /// <summary>
    /// The null expression - clears a scalar read model member back to no value.
    /// </summary>
    public const string Null = "$null";

    /// <summary>
    /// The composite key expression.
    /// </summary>
    public const string Composite = "$composite";

    /// <summary>
    /// The flat composite key expression - an untyped key whose parts are ordinary root properties of the read model.
    /// </summary>
    /// <remarks>
    /// A kernel that predates this expression routes a projection using it by event source identifier, so a read model
    /// identified by it needs a kernel that knows it on every silo that executes the projection.
    /// </remarks>
    public const string FlatComposite = "$flatComposite";

    /// <summary>
    /// The this accessor expression.
    /// </summary>
    public const string This = "$this";

    /// <summary>
    /// The identifier expression.
    /// </summary>
    public const string Id = "$id";

    /// <summary>
    /// The caused by expression.
    /// </summary>
    public const string CausedBy = "$causedBy";

    /// <summary>
    /// The split expression for event migrations.
    /// </summary>
    public const string Split = "$split";

    /// <summary>
    /// The default value expression for event migrations.
    /// </summary>
    public const string DefaultValue = "$defaultValue";

    /// <summary>
    /// The rename expression for event migrations.
    /// </summary>
    public const string Rename = "$rename";

    /// <summary>
    /// The combine expression for event migrations.
    /// </summary>
    public const string Combine = "$combine";

    /// <summary>
    /// The value map expression for event migrations - translates individual values of a property from what they
    /// meant in one generation to what they mean in another.
    /// </summary>
    public const string MapValues = "$mapValues";

    /// <summary>
    /// The all expression for subscribing to all event types.
    /// </summary>
    public const string All = "$all";
}
