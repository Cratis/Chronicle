// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents a command to remove an external service.
/// </summary>
public class RemoveExternalService
{
    /// <summary>
    /// Gets or sets the identifier of the external service to remove.
    /// </summary>
    public string ExternalServiceId { get; set; } = string.Empty;
}
