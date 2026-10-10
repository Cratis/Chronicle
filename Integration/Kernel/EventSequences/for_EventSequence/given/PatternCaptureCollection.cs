// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

[CollectionDefinition(Name)]
public class PatternCaptureCollection : ICollectionFixture<PatternCaptureFixture>
{
    public const string Name = "Pattern capture append subscription";
}
