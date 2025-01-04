// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle;

/// <summary>
/// Defines a system that can provide the different client artifacts.
/// </summary>
public interface IClientArtifactsProvider
{
    /// <summary>
    /// Gets all the available event types.
    /// </summary>
    IEnumerable<Type> EventTypes { get; }

    /// <summary>
    /// Gets all the available projection types.
    /// </summary>
    IEnumerable<Type> Projections { get; }

    /// <summary>
    /// Gets all the available Reactor types.
    /// </summary>
    IEnumerable<Type> Reactors { get; }

    /// <summary>
    /// Gets all the available reducer types.
    /// </summary>
    IEnumerable<Type> Reducers { get; }

    /// <summary>
    /// Gets all the available Reactor middleware types.
    /// </summary>
    IEnumerable<Type> ReactorMiddlewares { get; }

    /// <summary>
    /// Gets all the available providers of compliance metadata for types.
    /// </summary>
    IEnumerable<Type> ComplianceForTypesProviders { get; }

    /// <summary>
    /// Gets all the available providers of compliance metadata for properties.
    /// </summary>
    IEnumerable<Type> ComplianceForPropertiesProviders { get; }

    /// <summary>
    /// Gets all the available rule types.
    /// </summary>
    IEnumerable<Type> Rules { get; }

    /// <summary>
    /// Gets all the available event information provider types.
    /// </summary>
    IEnumerable<Type> AdditionalEventInformationProviders { get; }

    /// <summary>
    /// Gets all the available aggregate root types.
    /// </summary>
    IEnumerable<Type> AggregateRoots { get; }

    /// <summary>
    /// Gets all the available aggregate root state types.
    /// </summary>
    IEnumerable<Type> AggregateRootStateTypes { get; }

    /// <summary>
    /// Gets all the available constraint types represented by <see cref="IConstraint"/> .
    /// </summary>
    IEnumerable<Type> ConstraintTypes { get; }

    /// <summary>
    /// Gets all the available unique constraints represented by event types having properties with <see cref="UniqueAttribute"/>.
    /// </summary>
    IEnumerable<Type> UniqueConstraints { get; }

    /// <summary>
    /// Gets all the available unique event type constraints represented by event types having <see cref="UniqueAttribute"/>.
    /// </summary>
    IEnumerable<Type> UniqueEventTypeConstraints { get; }

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    void Initialize();
}
