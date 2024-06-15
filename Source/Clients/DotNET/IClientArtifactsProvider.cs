// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Gets all the available immediate projection types.
    /// </summary>
    IEnumerable<Type> ImmediateProjections { get; }

    /// <summary>
    /// Gets all the available adapters types.
    /// </summary>
    IEnumerable<Type> Adapters { get; }

    /// <summary>
    /// Gets all the available observer types.
    /// </summary>
    IEnumerable<Type> Observers { get; }

    /// <summary>
    /// Gets all the available reducer types.
    /// </summary>
    IEnumerable<Type> Reducers { get; }

    /// <summary>
    /// Gets all the available observer middleware types.
    /// </summary>
    IEnumerable<Type> ObserverMiddlewares { get; }

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
    IEnumerable<Type> AggregateRoots { get; }
}
