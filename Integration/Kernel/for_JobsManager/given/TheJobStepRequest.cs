// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

[GenerateSerializer]
[Alias("Cratis.Chronicle.Integration.Specifications.for_JobsManager.given.TheJobStepRequest")]
public record TheJobStepRequest([Id(0)] bool ShouldFail, [Id(1)] TimeSpan WaitTime, [Id(2)] int WaitCount);
