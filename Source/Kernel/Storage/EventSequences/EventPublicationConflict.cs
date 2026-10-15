// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// The exception that is thrown when a publication identity is reused for a different immutable intent.
/// </summary>
public class EventPublicationConflict() : Exception("The publication identity belongs to a different immutable intent.");
