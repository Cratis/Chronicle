// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers.Validators;

namespace Aksio.Cratis.Reducers;

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
        TypeMustBeAdornedWithReducerAttribute.ThrowIfReducerAttributeMissing(reducerType);

        /*
        Validation:
        + Must implement IReducerFor<T>
        + Shouldn't allow for multiple implementations of the IReducer interface for different read models
        + Must have a `[Reducer("id")]` attribute

        - If type is in nullable context, check if reducer methods use nullable for state, if not - error
        - Unique reducer ids
        - String keys need to match property names on the event / context
        - Composite keys need to match property names on the event
        - Types need to match between the different key definitions (can't have mixed key types)
        - Can't have multiple key definitions for the same event type
        - Should only allow one reduce method per event type

        Compile time checks:
        - Event type must be a valid event type
        - Check if read model type is nullable - if not, error
        */
    }
}
