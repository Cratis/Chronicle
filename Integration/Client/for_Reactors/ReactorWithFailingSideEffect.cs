// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

public class ReactorWithFailingSideEffect : IReactor
{
    public UniqueReactorSideEffect OnSomeEvent(SomeEvent evt) => new(evt.Number.ToString());
}
