// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
namespace Cratis.Chronicle.Grains.Jobs;

public class JobStepResult : Result<object?, PerformJobStepError>
{
    JobStepResult(Result<object?, PerformJobStepError> input)
        : base(input)
    {
    }

    public static JobStepResult Succeeded(object? input = default) => new(Result.Success<object?, PerformJobStepError>(input));
    public static JobStepResult Failed(PerformJobStepError input) => new(input);
}
