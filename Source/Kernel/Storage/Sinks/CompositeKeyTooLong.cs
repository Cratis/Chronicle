// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Exception that gets thrown when the encoded composite key of a read model instance does not fit the key column of the sink.
/// </summary>
/// <param name="readModel">The <see cref="ReadModelIdentifier"/> of the read model.</param>
/// <param name="length">The length of the encoded key.</param>
/// <param name="maximumLength">The maximum length the key column holds.</param>
public class CompositeKeyTooLong(ReadModelIdentifier readModel, int length, int maximumLength)
    : Exception($"The composite key of read model '{readModel}' encodes to {length} characters, which does not fit the key column of {maximumLength} characters. Use fewer or shorter key parts.");
