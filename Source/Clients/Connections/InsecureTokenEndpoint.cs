// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a token endpoint or authority is not an absolute https URL without user info.
/// </summary>
/// <param name="description">A description of the offending URL that carries no user info or query.</param>
public class InsecureTokenEndpoint(string description) :
    Exception($"The token endpoint or authority '{description}' must be an absolute https URL without user info.");
