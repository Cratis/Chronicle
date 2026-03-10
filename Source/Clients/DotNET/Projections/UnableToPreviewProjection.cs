// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when a projection preview fails.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToPreviewProjection"/> class.
/// </remarks>
/// <param name="errors">The collection of error messages describing why the preview failed.</param>
public class UnableToPreviewProjection(IEnumerable<string> errors)
    : Exception($"Unable to preview projection. Errors:\n{string.Join('\n', errors)}");
