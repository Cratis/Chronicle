// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A simple flat read model used to test single event source projections.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Name">Name.</param>
[Passive]
[FromEvent<ModuleCreated>]
public record SimpleModule(Guid Id, string Name);
