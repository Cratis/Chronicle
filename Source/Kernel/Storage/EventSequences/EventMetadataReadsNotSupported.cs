// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Thrown when a storage provider does not support metadata-only reads.
/// </summary>
public class EventMetadataReadsNotSupported() : Exception("This storage provider does not support metadata-only event reads.");
