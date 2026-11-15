// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A kernel that is not yet enrolled, with a hosted federation authority it can reach in the same process.
/// </summary>
public class a_peer_enrollment : a_federation_authority
{
    protected FakeEnrolledIdentities _identities;
    protected InProcessEnrollmentTransport _transport;
    protected CapturingLogger<FederationPeerEnrollment> _logger;
    protected FederationPeerEnrollment _peer;

    void Establish()
    {
        _identities = new();
        _logger = new();
        _transport = new(_authority, _keys.Authority);
        _peer = new(_identities, _transport, _time, _logger);
    }

    void Destroy() => _peer.Dispose();
}
