// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 16.45.x compatibility surface - do not remove.
//
// These messages were the client contract of the captures API up to and including 16.45.x and were
// dropped without a compatibility waiver when the contracts became generated (17.0.0). Compiled
// consumers hold assembly references to them - e.g. Cratis.Stage 3.11.0, whose generated type
// bindings reference Cratis.Chronicle.Contracts.Captures.Capture and fail to load with
// TypeLoadException when the type is absent. They carry no service methods: nothing on the wire
// calls them (the 17+ surface renamed the commands), but the types themselves must keep existing
// for as long as we serve 16.x-linked libraries, per the same "the newer contract still serves the
// older side" rule WireCompatibilityChecker enforces for the wire.
namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the payload for starting a capture.
/// </summary>
[ProtoContract]
public class StartCapture
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the capture.
    /// </summary>
    [ProtoMember(2)]
    public string Id { get; set; } = string.Empty;
}
