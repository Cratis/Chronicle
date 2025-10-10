// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks;

[IgnoreConvention]
public class InvokedWebhooks
{
    public List<HttpRequest> Requests = new();
}