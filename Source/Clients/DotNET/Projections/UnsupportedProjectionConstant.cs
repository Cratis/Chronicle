// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception thrown when a projection constant cannot be represented as a scalar literal.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnsupportedProjectionConstant"/> class.
/// </remarks>
/// <param name="targetProperty">The property receiving the constant.</param>
/// <param name="valueType">The unsupported type of the constant.</param>
public class UnsupportedProjectionConstant(PropertyPath targetProperty, Type valueType)
    : Exception($"Projection constant for property '{targetProperty}' has unsupported value type '{valueType.FullName}'. Only scalar constants are supported. Complex constant support is tracked at https://github.com/Cratis/Chronicle/issues/5525.");
