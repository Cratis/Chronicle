// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers.Validators;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerValidator"/>.
/// </summary>
public class ReducerValidator : IReducerValidator
{
    /// <inheritdoc/>
    public void Validate(Type reducerType)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(reducerType);
        TypeMustImplementOnlyOneReducer.ThrowIfTypeImplementsMoreThanOneReducer(reducerType);
    }
}
