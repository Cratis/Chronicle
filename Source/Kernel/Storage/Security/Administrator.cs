// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents a separately stored administrator privilege.
/// </summary>
/// <param name="Id">The user holding the privilege.</param>
/// <param name="Reason">The authority for the grant.</param>
/// <param name="Evidence">The supporting audit evidence.</param>
public record Administrator(UserId Id, GrantReason Reason, string Evidence);
