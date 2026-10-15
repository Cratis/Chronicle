// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_verifying_content;

/// <summary>
/// Resolves the verification result a storage backend can give.
/// </summary>
/// <remarks>
/// The SQL backends do not track whether a stored event has been revised, so they report every comparison as
/// unavailable rather than risk calling revised content equal. Only MongoDB can compare.
/// </remarks>
public static class ExpectedVerification
{
    /// <summary>
    /// Gets the expected result for a run, given the result a backend that tracks revisions reports.
    /// </summary>
    /// <param name="fixture">The <see cref="IChronicleFixture"/> for the run.</param>
    /// <param name="whenRevisionsAreTracked">The result when the backend tracks revisions.</param>
    /// <returns>The expected <see cref="ContentVerificationResult"/>.</returns>
    public static ContentVerificationResult For(IChronicleFixture fixture, ContentVerificationResult whenRevisionsAreTracked) =>
        fixture is ChronicleFixture { Options.StorageProvider: ChronicleStorageProvider.MongoDB }
            ? whenRevisionsAreTracked
            : ContentVerificationResult.Unavailable;
}
