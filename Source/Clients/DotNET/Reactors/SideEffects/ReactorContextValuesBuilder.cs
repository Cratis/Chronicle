// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Types;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesBuilder"/>.
/// </summary>
/// <param name="providers">The instances of <see cref="IReactorContextValuesProvider"/> to use when building the values.</param>
[Singleton]
public class ReactorContextValuesBuilder(IInstancesOf<IReactorContextValuesProvider> providers) : IReactorContextValuesBuilder
{
    /// <inheritdoc/>
    public ReactorContextValues Build(object reactor, EventContext eventContext)
    {
        var values = new ReactorContextValues();
        foreach (var provider in providers)
        {
            values.Merge(provider.Provide(reactor, eventContext));
        }

        return values;
    }
}
