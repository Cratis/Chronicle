// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents an event the projection could not apply to the graph while previewing.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the event.</param>
/// <param name="Message">Why the event could not be applied.</param>
public record GraphPreviewEventError(ulong SequenceNumber, string Message);
