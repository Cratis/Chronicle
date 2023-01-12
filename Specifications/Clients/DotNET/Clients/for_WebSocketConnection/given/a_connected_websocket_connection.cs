// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients.given;

public class a_connected_websocket_connection : a_responding_kernel
{
    async Task Establish()
    {
        await connection.Connect();
    }
}
