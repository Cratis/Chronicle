// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a reactor that can provide the <see cref="Subject"/> to associate with side-effect events
/// returned from handler methods.
/// </summary>
/// <remarks>
/// The subject identifies the data owner (e.g. a customer) for PII encryption purposes.
/// Implement this interface on a reactor class to supply a subject automatically; otherwise
/// no explicit subject is set and Chronicle will attempt to derive it from the event itself.
/// </remarks>
public interface ICanProvideSubject
{
    /// <summary>
    /// Gets the <see cref="Subject"/> to associate with side-effect events.
    /// </summary>
    /// <returns>The <see cref="Subject"/>.</returns>
    Subject GetSubject();
}
