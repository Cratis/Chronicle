// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Represents an implementation of <see cref="IChronicleAspireBuilder"/>.
/// </summary>
/// <param name="resourceBuilder">The underlying <see cref="IResourceBuilder{T}"/> for the Chronicle resource.</param>
internal sealed class ChronicleAspireBuilder(IResourceBuilder<ChronicleResource> resourceBuilder) : IChronicleAspireBuilder
{
    /// <inheritdoc/>
    public IResourceBuilder<ChronicleResource> ResourceBuilder { get; } = resourceBuilder;
}
