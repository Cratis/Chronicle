// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

/// <summary>
/// Immediate projection that replaces only the Orleans self-reference conversion unsupported by TestKit.
/// </summary>
/// <param name="storage">Storage used by the projection.</param>
/// <param name="expandoObjectConverter">Converter used for read model state.</param>
/// <param name="logger">Logger used by the projection.</param>
public class TestableImmediateProjection(
    Storage.IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ImmediateProjection> logger) : ImmediateProjection(storage, expandoObjectConverter, logger), IGrainType
{
    readonly INotifyProjectionDefinitionsChanged _selfReference = Substitute.For<INotifyProjectionDefinitionsChanged>();

    /// <inheritdoc/>
    public Type GrainType => typeof(IImmediateProjection);

    /// <inheritdoc/>
    protected override INotifyProjectionDefinitionsChanged GetSelfGrainReference() => _selfReference;
}
