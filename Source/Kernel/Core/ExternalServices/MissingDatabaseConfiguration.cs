// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// The exception that is thrown when a database endpoint is missing its database configuration.
/// </summary>
/// <param name="type">The <see cref="ExternalServiceEndpointType"/> that is missing configuration.</param>
public class MissingDatabaseConfiguration(ExternalServiceEndpointType type)
    : Exception($"The endpoint of type '{type}' is missing its database configuration");
