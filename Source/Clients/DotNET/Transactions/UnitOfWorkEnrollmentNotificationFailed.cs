// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when committing a unit whose enrollment notification failed.
/// </summary>
/// <param name="cause">The failure that prevented recording the staged enrollment.</param>
public class UnitOfWorkEnrollmentNotificationFailed(Exception cause)
    : Exception("The unit of work cannot commit because an enrollment notification failed. Roll it back or dispose it.", cause);
