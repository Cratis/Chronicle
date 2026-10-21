// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Records the receiver policy that made a webhook failure eligible for health recovery.
/// Missing values on legacy failures intentionally grant no automatic recovery authority.
/// </summary>
public enum WebhookFailureDisposition
{
    /// <summary>The receiver failed transiently and exhausted its retry budget.</summary>
    Retryable = 0,

    /// <summary>The receiver rejected delivery with a configuration error.</summary>
    Configuration = 1,

    /// <summary>The receiver returned 410 and requires operator action.</summary>
    Gone = 2
}
