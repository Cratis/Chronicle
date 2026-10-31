// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

/// <summary>
/// Immediate projection that replaces only the Orleans self-reference conversion unsupported by TestKit.
/// </summary>
/// <param name="storage">Storage used by the projection.</param>
/// <param name="expandoObjectConverter">Converter used for read model state.</param>
/// <param name="visibilities">Visibilities used by the projection.</param>
/// <param name="logger">Logger used by the projection.</param>
public class TestableImmediateProjection(
    Storage.IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    IReadModelInstanceVisibilities visibilities,
    ILogger<ImmediateProjection> logger) : ImmediateProjection(storage, expandoObjectConverter, visibilities, logger), IGrainType
{
    readonly INotifyProjectionDefinitionsChanged _selfReference = Substitute.For<INotifyProjectionDefinitionsChanged>();

    /// <inheritdoc/>
    public Type GrainType => typeof(IImmediateProjection);

    /// <summary>
    /// Gets whether session deactivation was requested.
    /// </summary>
    public bool SessionDeactivated { get; private set; }

    /// <inheritdoc/>
    protected override void DeactivateSession() => SessionDeactivated = true;

    /// <inheritdoc/>
    protected override INotifyProjectionDefinitionsChanged GetSelfGrainReference() => _selfReference;
}
