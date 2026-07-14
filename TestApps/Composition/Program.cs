// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Aspire.Hosting.Yarp;
using Yarp.ReverseProxy.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Chronicle needs MongoDB transactions (AppendMany, unit of work, Orleans membership), which
// require a replica set - cratis/mongodb is a single node replica set image. The connection
// string uses directConnection so host clients skip replica set host discovery (the member
// advertises its in-container address).
const string MongoConnectionString = "mongodb://localhost:27019/?directConnection=true";
var mongodb = builder.AddContainer("mongodb", "cratis/mongodb")
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.Port = 27019;
        endpoint.TargetPort = 27017;
        endpoint.IsProxied = false;
    });

// CoreDNS serving the chronicle.local zone with the _chronicle._tcp SRV records that point at the
// two kernels - this is what the chronicle+srv:// connection string resolves against.
builder.AddContainer("dns", "coredns/coredns", "1.12.1")
    .WithBindMount("dns", "/config", isReadOnly: true)
    .WithArgs("-conf", "/config/Corefile")
    .WithEndpoint("dns", endpoint =>
    {
        endpoint.Port = 8053;
        endpoint.TargetPort = 53;
        endpoint.Protocol = ProtocolType.Udp;
        endpoint.IsProxied = false;
    });

var kernel1 = AddKernel("kernel-1", port: 35001, siloPort: 11111, gatewayPort: 30001);
var kernel2 = AddKernel("kernel-2", port: 35002, siloPort: 11112, gatewayPort: 30002);

// The web apps resolve the kernels through DNS SRV - the srvNameServer option points the lookup
// at the CoreDNS container above.
const string SrvConnectionString =
    "chronicle+srv://chronicle-dev-client:chronicle-dev-secret@chronicle.local/?srvNameServer=127.0.0.1:8053";

AddWebApp("app-1", port: 5101);
AddWebApp("app-2", port: 5102);

// Every kernel already hosts its own Workbench (UI + API) on its main port - there is no separate
// Workbench app to run. "One load-balanced Workbench" therefore means fronting the two kernels'
// identical, cluster-backed Workbench with a reverse proxy, not standing up a third host that
// round-robins its own connection. YARP round-robins across both; each kernel's dev certificate is
// self-signed, so the proxy is told to accept it rather than validate against a CA.
builder.AddYarp("workbench")
    .WithHostPort(9876)

    // The multi-destination AddCluster(name, destinations) overload below doesn't call
    // WithReference itself (unlike the single-destination overloads), so the container never
    // gets the services__kernel-N__https__0 env vars it needs to resolve https://kernel-1 /
    // https://kernel-2 - without these, YARP tries (and fails) a literal DNS lookup for
    // "kernel-1" as a hostname.
    .WithReference(kernel1)
    .WithReference(kernel2)
    .WithConfiguration(yarp =>
    {
        var kernels = yarp.AddCluster("kernels", [kernel1.GetEndpoint("https"), kernel2.GetEndpoint("https")])
            .WithLoadBalancingPolicy("RoundRobin")
            .WithHttpClientConfig(new HttpClientConfig { DangerousAcceptAnyServerCertificate = true });
        yarp.AddRoute("/{**catch-all}", kernels);
    })
    .WaitFor(kernel1)
    .WaitFor(kernel2);

await builder.Build().RunAsync();

IResourceBuilder<ProjectResource> AddKernel(string name, int port, int siloPort, int gatewayPort) =>
    builder.AddProject<Projects.Server>(name)

        // The kernel binds this port itself (a single custom Kestrel listener multiplexing
        // gRPC and HTTP/1.1) rather than through the usual ASPNETCORE_URLS convention, so DCP
        // must not also proxy it - a proxied endpoint here creates a second, conflicting
        // listener on the same port that never forwards anywhere.
        .WithEndpoint("https", endpoint =>
        {
            endpoint.Port = port;
            endpoint.TargetPort = port;
            endpoint.UriScheme = "https";
            endpoint.IsProxied = false;
        })
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("Cratis__Chronicle__Port", port.ToString())
        .WithEnvironment("Cratis__Chronicle__Storage__Type", "MongoDB")
        .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", MongoConnectionString)
        .WithEnvironment("Cratis__Chronicle__Clustering__Type", "MongoDB")
        .WithEnvironment("Cratis__Chronicle__Clustering__SiloPort", siloPort.ToString())
        .WithEnvironment("Cratis__Chronicle__Clustering__GatewayPort", gatewayPort.ToString())
        .WithEnvironment("Cratis__Chronicle__Clustering__AdvertisedIP", "127.0.0.1")
        .WaitFor(mongodb);

IResourceBuilder<ProjectResource> AddWebApp(string name, int port) =>
    builder.AddProject<Projects.AspNetCore>(name)
        .WithHttpEndpoint(port: port)
        .WithEnvironment("INSTANCE_NAME", name)
        .WithEnvironment("Cratis__Chronicle__ConnectionString", SrvConnectionString)
        .WaitFor(kernel1)
        .WaitFor(kernel2);
