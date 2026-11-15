// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation binding is invalid.
/// </summary>
/// <param name="id">The binding id.</param>
/// <param name="reason">Why the binding is invalid. Never contains a secret.</param>
public class InvalidFederationBinding(string id, string reason) : Exception($"The federation binding '{id}' is invalid: {reason}");
