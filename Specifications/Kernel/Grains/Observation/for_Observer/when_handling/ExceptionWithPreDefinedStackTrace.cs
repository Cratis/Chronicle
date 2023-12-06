// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class ExceptionWithPreDefinedStackTrace : Exception
{
    public ExceptionWithPreDefinedStackTrace(string message, string stackTrace) : base(message)
    {
        StackTrace = stackTrace;
    }

    public override string StackTrace { get; }
}
