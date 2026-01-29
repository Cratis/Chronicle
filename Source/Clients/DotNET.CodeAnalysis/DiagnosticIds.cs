// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Diagnostic IDs for Chronicle analyzers.
/// </summary>
public static class DiagnosticIds
{
    /// <summary>
    /// Event type must have EventType attribute when appended to event sequence.
    /// </summary>
    public const string EventTypeMustHaveAttributeWhenAppended = "CHR0001";

    /// <summary>
    /// Declarative projection generic arguments must have EventType attribute.
    /// </summary>
    public const string DeclarativeProjectionEventTypeMustHaveAttribute = "CHR0002";

    /// <summary>
    /// Model bound projection attributes must reference types with EventType attribute.
    /// </summary>
    public const string ModelBoundProjectionEventTypeMustHaveAttribute = "CHR0003";

    /// <summary>
    /// Reactor method signature must match allowed signatures.
    /// </summary>
    public const string ReactorMethodSignatureMustMatchAllowed = "CHR0004";

    /// <summary>
    /// Reactor event parameter must have EventType attribute.
    /// </summary>
    public const string ReactorEventParameterMustHaveAttribute = "CHR0005";

    /// <summary>
    /// Reducer method signature must match allowed signatures.
    /// </summary>
    public const string ReducerMethodSignatureMustMatchAllowed = "CHR0006";

    /// <summary>
    /// Reducer event parameter must have EventType attribute.
    /// </summary>
    public const string ReducerEventParameterMustHaveAttribute = "CHR0007";
}
