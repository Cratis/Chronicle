// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Factory delegate that can create <see cref="ISinks"/>.
/// </summary>
/// <param name="eventStoreNamespaceName">The <see cref="EventStoreNamespaceName"/> to use.</param>
/// <returns>The created <see cref="ISinks"/> instance.</returns>
public delegate ISinks SinksFactory(EventStoreNamespaceName eventStoreNamespaceName);
