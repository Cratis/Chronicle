// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that wraps a <see cref="ReactorSideEffectFailure"/> for use in the Catch monad.
/// </summary>
/// <remarks>
/// This exception is used internally to propagate side-effect failures through the reactor pipeline
/// as part of the Catch monad, allowing structured error handling with the Result pattern.
/// </remarks>
/// <param name="failure">The <see cref="ReactorSideEffectFailure"/> containing failure details.</param>
public class ReactorSideEffectException(ReactorSideEffectFailure failure) : Exception("Reactor side-effect failed")
{
    /// <summary>
    /// Gets the <see cref="ReactorSideEffectFailure"/> containing the details of what failed.
    /// </summary>
    public ReactorSideEffectFailure Failure { get; } = failure;
}
