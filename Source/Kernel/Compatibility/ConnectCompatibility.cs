// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Checks whether a client and a kernel can still talk to each other on connect, regardless of which one
/// happens to be newer.
/// </summary>
/// <remarks>
/// <see cref="WireCompatibilityChecker.Check"/> always answers one question - does the newer contract still
/// serve the older one - and it has to be told which contract that is. A kernel is upgraded on its own
/// schedule, and so is any client that talks to it, so on any given connection either side can be the one
/// that has moved on. Getting this backwards is not a theoretical risk: it shipped. A kernel once compared
/// the client it had just been handed as the older side unconditionally, so a client that had gained a field
/// since the kernel's release was told that field had been removed and refused a connection it was fully
/// able to make (#4058).
/// <para>
/// Both sides already report the same release-stamped protocol version string - <c language="csharp">Cratis.Chronicle.Contracts.ProtocolVersion.Current</c>
/// on the kernel, the equivalent client SDK property on the way in - so ordering by that is enough to pick
/// the right direction without any new information changing hands or any change on the client's part.
/// </para>
/// </remarks>
public static class ConnectCompatibility
{
    /// <summary>
    /// Checks whether a client and a kernel still serve each other.
    /// </summary>
    /// <param name="clientContract">The wire contract the client was built against.</param>
    /// <param name="clientProtocolVersion">The client's protocol version.</param>
    /// <param name="kernelContract">The wire contract the kernel currently serves.</param>
    /// <param name="kernelProtocolVersion">The kernel's protocol version.</param>
    /// <returns>A <see cref="WireCompatibilityReport"/> naming what the older of the two would lose.</returns>
    public static WireCompatibilityReport Check(
        WireContract clientContract,
        string clientProtocolVersion,
        WireContract kernelContract,
        string kernelProtocolVersion) =>
        ProtocolVersionOrder.Compare(clientProtocolVersion, kernelProtocolVersion) > 0

            // The client is newer than the kernel: the kernel is the older side, so ask whether the client
            // still serves what the kernel expects. This is the direction the incident needed and the
            // original code never asked.
            ? WireCompatibilityChecker.Check(kernelContract, clientContract)

            // The kernel is newer than the client, or the two report the same version: the client is the
            // older side (or ties don't matter), so ask whether the kernel still serves what the client
            // expects. This is the direction Chronicle has always checked.
            : WireCompatibilityChecker.Check(clientContract, kernelContract);
}
