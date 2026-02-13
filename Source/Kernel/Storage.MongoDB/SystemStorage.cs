// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISystemStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
public class SystemStorage(IDatabase database) : ISystemStorage
{
    /// <inheritdoc/>
    public IUserStorage Users { get; } = new UserStorage(database);

    /// <inheritdoc/>
    public IApplicationStorage Applications { get; } = new ApplicationStorage(database);

    /// <inheritdoc/>
    public IDataProtectionKeyStorage DataProtectionKeys { get; } = new DataProtectionKeyStorage(database);
}
