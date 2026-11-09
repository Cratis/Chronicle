// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation binding does not exist.
/// </summary>
/// <param name="id">The binding id.</param>
public class FederationBindingNotFound(string id) : Exception($"There is no federation binding '{id}'");
