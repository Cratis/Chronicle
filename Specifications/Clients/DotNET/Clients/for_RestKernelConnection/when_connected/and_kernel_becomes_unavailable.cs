// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients.for_RestKernelConnection.when_connected;

public class and_kernel_becomes_unavailable : given.a_connected_connection
{
    bool has_failed_once;

    protected override HttpResponseMessage GetMessageForRoute(string route)
    {
        if (route == ping_route && !has_failed_once)
        {
            has_failed_once = true;
            return not_found_message;
        }

        return success_message;
    }

    void Establish() => has_failed_once = false;

    [Fact] void client_should_notify_client_lifecycle_about_being_disconnected() => connection_lifecycle.Verify(_ => _.Disconnected(), Once);
}
