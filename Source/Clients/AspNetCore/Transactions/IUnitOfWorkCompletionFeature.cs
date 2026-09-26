// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>Allows an action to ask the request middleware to complete its unit of work before writing a response.</summary>
public interface IUnitOfWorkCompletionFeature
{
    /// <summary>Commits the request unit using the middleware's ownership capability.</summary>
    /// <returns>The completed unit, whose outcome and decision conflicts can be inspected.</returns>
    Task<IUnitOfWork> CommitAsync();
}
