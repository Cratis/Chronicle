// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The name shared by the <c language="csharp">[Unique]</c> attributes on <see cref="PhoneClaimed"/> and <see cref="EmailClaimed"/>.
/// </summary>
public static class ContactClaim
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "OneContactClaimPerEventSource";
}
