// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// An immutable snapshot of a token and the identity it was acquired for.
/// </summary>
/// <param name="AccessToken">The acquired access token.</param>
/// <param name="Binding">The token's verified binding, or null when unknown.</param>
internal sealed record TokenWithBinding(string AccessToken, TokenCacheBinding? Binding);
