// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Converts stored applications into the application read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class ApplicationConverters
{
    /// <summary>
    /// Converts a stored application into the read model.
    /// </summary>
    /// <param name="app">The stored application.</param>
    /// <returns>The application read model.</returns>
    internal static Application ToApplication(Storage.Security.Application app) =>
        new(
            (Guid)app.Id,
            (string)app.ClientId,
            true,
            DateTimeOffset.UtcNow,
            null);
}
