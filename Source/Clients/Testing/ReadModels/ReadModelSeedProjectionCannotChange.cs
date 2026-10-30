// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A projection cannot be replaced after a named seed has established its key contract.
/// </summary>
/// <param name="readModelType">The seeded read model.</param>
public class ReadModelSeedProjectionCannotChange(Type readModelType)
    : Exception($"Cannot replace the projection for read model '{readModelType.FullName}' after a named key has been seeded. Create another scenario to use a different projection.");
