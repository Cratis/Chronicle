// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents a unique key for an adapter.
/// </summary>
/// <param name="Model">Type of model the key is for.</param>
/// <param name="ExternalModel">Type of external model the key is for.</param>
public record AdapterKey(Type Model, Type ExternalModel);
