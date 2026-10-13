// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a self-reducing read model has conflicting builder ownership.
/// </summary>
/// <param name="readModelType">The read model with conflicting ownership.</param>
/// <param name="builders">The conflicting builder types.</param>
public class AmbiguousReadModelBuilder(Type readModelType, IEnumerable<Type> builders)
    : Exception($"Self-reducing read model '{readModelType.FullName}' has conflicting builders: {string.Join(", ", builders.Select(type => type.FullName).Distinct().Order(StringComparer.Ordinal))}.");
