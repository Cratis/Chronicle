// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public record TheJobStepRequest(bool ShouldFail, TimeSpan WaitTime, int WaitCount);
