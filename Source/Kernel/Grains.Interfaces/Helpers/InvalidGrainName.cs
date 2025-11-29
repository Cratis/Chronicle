// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans;

/// <summary>
/// Exception that gets thrown when a grain name is invalid for a job.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidGrainName"/> class.
/// </remarks>
/// <param name="grainType">Violating grain type.</param>
public class InvalidGrainName(Type grainType) : Exception($"Grain type '{grainType.Name}' is invalid. No interface for the grain was matched. It should follow the convention of `IFoo` -> `Foo`.")
{
}
