// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Defines a builder for configuring a Chronicle resource in an Aspire distributed application.
/// </summary>
public interface IChronicleAspireBuilder
{
    /// <summary>
    /// Gets the underlying resource builder for the Chronicle resource.
    /// </summary>
    IResourceBuilder<ChronicleResource> ResourceBuilder { get; }
}
