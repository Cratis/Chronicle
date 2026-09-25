// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// The exception that is thrown when a system projection property accessor does not name a read model property.
/// </summary>
/// <param name="readModelType">The read model type the accessor was declared on.</param>
/// <param name="expression">The accessor expression as written.</param>
public class InvalidKernelProjectionPropertyAccessor(Type readModelType, string expression)
    : Exception($"Accessor '{expression}' on read model '{readModelType.FullName}' does not name a property. A system projection extracts the property path from the accessor at definition time rather than executing it, so it has to be a member access rooted in the lambda parameter - for example 'model => model.Count'.");
