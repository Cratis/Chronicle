// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line keyed by an <see cref="int"/> bucket number.
/// </summary>
/// <param name="Bucket">The bucket number, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
public record BucketLine(
    int Bucket,
    decimal Amount);
