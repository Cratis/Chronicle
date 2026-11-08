// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// The kinds of observer whose batch methods the analyzers check.
/// </summary>
internal enum BatchObserverKind
{
    /// <summary>
    /// The type is not an observer.
    /// </summary>
    None = 0,

    /// <summary>
    /// The type is a reactor.
    /// </summary>
    Reactor = 1,

    /// <summary>
    /// The type is a reducer implementing IReducerFor or registered as a reducer.
    /// </summary>
    Reducer = 2,

    /// <summary>
    /// The type is a self-reducing read model.
    /// </summary>
    SelfReducer = 3
}
