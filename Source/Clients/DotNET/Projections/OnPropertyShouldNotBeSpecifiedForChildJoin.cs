// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when on property should not be specified for child join.
/// </summary>
/// <param name="readModelType">Type of read model joining with event.</param>
/// <param name="eventType">Type of event that is being joined.</param>
public class OnPropertyShouldNotBeSpecifiedForChildJoin(Type readModelType, Type eventType)
    : Exception($"On property should not be specified for child join for model of type '{readModelType.FullName}' when joining with event of type '{eventType.FullName}', it will implicitly use what is set using the `IdentifiedBy()");
