// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a numbered bucket, keyed by an <see cref="int"/>.
/// </summary>
/// <param name="Bucket">The bucket number, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record BucketAmountRecorded(int Bucket, decimal Amount);
