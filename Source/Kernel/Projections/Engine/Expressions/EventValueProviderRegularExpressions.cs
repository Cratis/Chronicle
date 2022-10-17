// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Regular expressions representing event value providers.
/// </summary>
public static class EventValueProviderRegularExpressions
{
    /// <summary>
    /// Gets the expression that represents the event value provider as a parameter.
    /// </summary>
    public const string Expression = "[A-Za-z.$]";
}
