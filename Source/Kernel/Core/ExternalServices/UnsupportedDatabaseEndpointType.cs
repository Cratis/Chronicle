// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// The exception that is thrown when there is no connection-string formatter for a database endpoint type.
/// </summary>
/// <param name="type">The <see cref="ExternalServiceEndpointType"/> that is unsupported.</param>
public class UnsupportedDatabaseEndpointType(ExternalServiceEndpointType type)
    : Exception($"There is no connection-string formatter registered for endpoint type '{type}'");
