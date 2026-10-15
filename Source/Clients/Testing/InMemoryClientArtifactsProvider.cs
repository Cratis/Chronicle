// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a per-run artifact catalog populated by the test author, without assembly discovery.
/// </summary>
public class InMemoryClientArtifactsProvider : IClientArtifactsProvider
{
    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> Projections { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForTypesProviders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> SecurityForPropertiesProviders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> ClosesStreamEventTypes { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders { get; set; } = [];

    /// <inheritdoc/>
    public IEnumerable<Type> EventSources { get; set; } = [];
}
