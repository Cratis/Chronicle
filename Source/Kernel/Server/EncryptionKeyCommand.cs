// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The command line options for inspecting encryption keys stored under earlier names.
/// </summary>
/// <param name="Verify">Whether the whole key store is inspected and the process exits afterwards.</param>
/// <param name="RemainingArgs">The arguments left for the host.</param>
internal record EncryptionKeyCommand(bool Verify, string[] RemainingArgs)
{
    /// <summary>
    /// The flag that inspects the whole key store and exits.
    /// </summary>
    internal const string VerifyFlag = "--verify-encryption-keys";

    /// <summary>
    /// Parses the encryption key options out of the arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The <see cref="EncryptionKeyCommand"/>.</returns>
    internal static EncryptionKeyCommand Parse(string[] args) =>
        new(args.Contains(VerifyFlag, StringComparer.Ordinal), args.Where(argument => argument != VerifyFlag).ToArray());
}
