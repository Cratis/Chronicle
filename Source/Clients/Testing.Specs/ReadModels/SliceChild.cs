// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A leaf slice child model.
/// </summary>
/// <param name="Id">The slice identifier used as the key.</param>
/// <param name="Name">Slice name.</param>
public record SliceChild(
    [Key] Guid Id,
    string Name);
