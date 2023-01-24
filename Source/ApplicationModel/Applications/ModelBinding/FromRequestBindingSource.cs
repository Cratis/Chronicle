// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.ModelBinding;

/// <summary>
/// Represents a binding source for a binding type that allows an object to be bound in a combination of values on the HTTP body, route and querystring.
/// </summary>
public class FromRequestBindingSource : BindingSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FromRequestBindingSource"/> class.
    /// </summary>
    public FromRequestBindingSource()
        : base("65e9676d-9653-4567-9496-fe209de19589", "FromRequest", true, true)
    {
    }
}
