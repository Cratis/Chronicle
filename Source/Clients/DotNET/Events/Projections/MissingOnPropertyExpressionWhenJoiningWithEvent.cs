// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Exception that is thrown when missing the on property expression when joining with specific event type.
/// </summary>
public class MissingOnPropertyExpressionWhenJoiningWithEvent : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingOnPropertyExpressionWhenJoiningWithEvent"/> class.
    /// </summary>
    /// <param name="modelType">Type of model joining with event.</param>
    /// <param name="eventType">Type of event that is being joined</param>
    public MissingOnPropertyExpressionWhenJoiningWithEvent(Type modelType, Type eventType) : base($"Missing the on property expression for model of type '{modelType.FullName}' when joining with event of type '{eventType.FullName}'")
    {
    }

}
