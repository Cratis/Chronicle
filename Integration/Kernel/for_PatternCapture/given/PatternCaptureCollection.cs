// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.given;

[CollectionDefinition(Name, DisableParallelization = true)]
public class PatternCaptureCollection : ICollectionFixture<PatternCaptureFixture>
{
    public const string Name = "Pattern capture";
}
