// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a read model that takes part in a variant group is registered explicitly.
/// </summary>
/// <param name="readModelType">The read model type that was registered.</param>
public class VariantReadModelsCannotBeRegisteredExplicitly(Type readModelType)
    : Exception($"Read model '{readModelType.FullName}' is a variant, or a global handler of one. Variants are wired together with their siblings during discovery and cannot be registered one at a time.");
