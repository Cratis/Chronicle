// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesProvider"/> that provides the subject for event appends.
/// </summary>
/// <remarks>
/// Resolves the <see cref="Subject"/> from a reactor implementing <see cref="ICanProvideSubject"/>.
/// No value is provided when the reactor does not implement the interface.
/// </remarks>
public class SubjectValuesProvider : IReactorContextValuesProvider
{
    /// <inheritdoc/>
    public ReactorContextValues Provide(object reactor, EventContext eventContext)
    {
        if (reactor is ICanProvideSubject provider)
        {
            return new ReactorContextValues
            {
                { WellKnownReactorContextKeys.Subject, provider.GetSubject() }
            };
        }

        return [];
    }
}
