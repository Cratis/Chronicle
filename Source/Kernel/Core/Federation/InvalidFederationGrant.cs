// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation grant is invalid.
/// </summary>
/// <param name="id">The link id.</param>
/// <param name="reason">Why the grant is invalid. Never contains a secret.</param>
public class InvalidFederationGrant(string id, string reason) : Exception($"The federation grant '{id}' is invalid: {reason}");
