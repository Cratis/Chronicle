// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when an exemption is declared for a name the resource name rule accepts anyway.
/// </summary>
/// <param name="name">The name.</param>
public class UnusedResourceNameExemption(string name) : Exception($"The exemption '{name}' is not needed because the resource name rule accepts it.");
