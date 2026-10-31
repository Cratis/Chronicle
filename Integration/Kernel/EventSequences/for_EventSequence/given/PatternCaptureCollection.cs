// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

/// <summary>
/// Serializes the dedicated host with other collections that share the process-wide client artifacts provider.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class PatternCaptureCollection : ICollectionFixture<PatternCaptureFixture>
{
    public const string Name = "Pattern capture append subscription";
}
