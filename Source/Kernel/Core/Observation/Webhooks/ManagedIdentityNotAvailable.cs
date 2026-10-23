// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when managed identity authorization is not available in this host or surface.
/// </summary>
/// <param name="message">The reason managed identity is unavailable.</param>
public class ManagedIdentityNotAvailable(string message = "Managed identity webhook token acquisition is not available in this host.") : Exception(message);
