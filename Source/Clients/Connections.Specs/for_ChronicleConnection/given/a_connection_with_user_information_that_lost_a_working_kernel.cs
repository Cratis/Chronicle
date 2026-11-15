// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection that lost a working kernel and is configured with user information.
/// </summary>
public class a_connection_with_user_information_that_lost_a_working_kernel : a_connection_that_lost_a_working_kernel
{
    protected override ChronicleConnectionString ConnectionString => new("chronicle://user:s3cret@localhost:35000");
}
