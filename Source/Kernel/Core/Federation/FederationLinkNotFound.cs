// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation link (producer grant) does not exist.
/// </summary>
/// <param name="id">The link id.</param>
public class FederationLinkNotFound(string id) : Exception($"There is no federation link '{id}'");
