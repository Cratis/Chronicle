// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents the command for creating a read model.
/// </summary>
/// <param name="Name">Name of the read model to create.</param>
public record CreateReadModel(string Name);
