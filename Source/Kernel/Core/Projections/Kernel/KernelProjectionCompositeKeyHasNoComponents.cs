// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// The exception that is thrown when a system projection declares a composite key without any components.
/// </summary>
/// <param name="readModelType">The read model type the composite key was declared for.</param>
public class KernelProjectionCompositeKeyHasNoComponents(Type readModelType)
    : Exception($"The composite key for read model '{readModelType.FullName}' has no components. A composite key with no components resolves to no key at all, which collapses every instance onto one - add at least one component with 'With(...)'.");
