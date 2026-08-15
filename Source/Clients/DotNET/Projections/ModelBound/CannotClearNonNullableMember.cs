// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a clear is declared for a read model member that cannot hold null.
/// </summary>
/// <remarks>
/// Clearing means returning a member to no value. A member declared non-nullable has no such state, and writing its
/// type default instead - an empty string, a zero - is a different fact that the read model cannot tell apart from a
/// real value. The declaration is therefore rejected rather than reinterpreted.
/// </remarks>
/// <param name="declaringType">The read model type declaring the member.</param>
/// <param name="memberName">The name of the member the clear was declared for.</param>
/// <param name="memberType">The declared type of the member.</param>
public class CannotClearNonNullableMember(Type declaringType, string memberName, Type memberType)
    : Exception($"Member '{memberName}' on '{declaringType.FullName}' is declared as '{memberType.Name}', which cannot hold null, so it cannot be cleared. Declare the member as nullable, or use [SetValue<TEvent>(...)] with the value you actually want it to hold.");
