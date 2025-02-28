// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Exception that gets thrown when a <see cref="JobType"/> is not associated with the given <see cref="Type"/>.
/// </summary>
/// <param name="type">The type.</param>
public class JobTypeNotAssociatedWithType(Type type)
    : Exception($"There is no JobType associated with {type}");
