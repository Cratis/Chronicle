// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Delegate representing a callback for attempting to resolve execution context.
/// </summary>
/// <param name="executionContext">Out result of resolution.</param>
/// <returns>True if it was able to resolve, false if not.</returns>
public delegate bool ExecutionContextResolver(out ExecutionContext executionContext);
