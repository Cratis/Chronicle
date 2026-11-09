// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A secured connection to one producer.
/// </summary>
public interface IFederationPullConnection : IDisposable
{
    /// <summary>
    /// Gets the pull service of the producer.
    /// </summary>
    IFederationPull Service { get; }
}
