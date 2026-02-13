// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.given;

public class a_reducer_observers : Specification
{
    protected ReducerObservers _observers;

    void Establish() => _observers = new ReducerObservers();
}
