// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

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
    /// The value expression.
    /// </summary>
    public const string Value = "$value";

    /// <summary>
    /// The count expression.
    /// </summary>
    public const string Count = "$count";

    /// <summary>
    /// The composite key expression.
    /// </summary>
    public const string Composite = "$composite";

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
}
