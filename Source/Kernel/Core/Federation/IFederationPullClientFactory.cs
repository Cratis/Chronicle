// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates the connection a consumer pulls over.
/// </summary>
public interface IFederationPullClientFactory
{
    /// <summary>
    /// Create a connection to a binding's producer.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <returns>The connection; dispose it to close the channel.</returns>
    IFederationPullConnection Create(FederationBindingState binding);
}
