// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Aspire.Hosting.Yarp;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.SessionAffinity;

// Fixed port and no login token, purely for local dev convenience - without ASPNETCORE_URLS the
// dashboard binds a random port every run, and without this flag every visit needs the ?t=<token>
// query string from the startup log.
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");

var builder = DistributedApplication.CreateBuilder(args);

// Chronicle needs MongoDB transactions (AppendMany, unit of work, Orleans membership), which
// require a replica set. mongo:8 has no bundled replica set, so start mongod with replSet enabled,
// wait for it, run rs.initiate() with the member advertising localhost:27017, then tail to keep the
// container alive. The connection string uses directConnection so host clients skip replica set host
// discovery (the member advertises its in-container address).
const string MongoReplicaSetInit =
    "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & " +
    "until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; " +
    "mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; " +
    "tail -f /dev/null";
const string MongoConnectionString = "mongodb://localhost:27019/?directConnection=true";
var mongodb = builder.AddContainer("mongodb", "mongo", "8")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", MongoReplicaSetInit)
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
// at the CoreDNS container above. loadBalancer is spelled out explicitly even though
// least-connections is the default, so the composition demonstrates the option by example.
// skipTlsValidation is needed because the kernels serve self-signed development certificates.
const string SrvConnectionString =
    "chronicle+srv://chronicle-dev-client:chronicle-dev-secret@chronicle.local/?srvNameServer=127.0.0.1:8053&loadBalancer=least-connections&skipTlsValidation=true";

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
            .WithHttpClientConfig(new HttpClientConfig { DangerousAcceptAnyServerCertificate = true })

            // The Workbench's observable queries open an SSE stream to get a connectionId, then
            // POST to /subscribe with that id as a *separate* request - without affinity,
            // round-robin can send that POST to the other kernel, which has never heard of the
            // connection, so it fails and the client reconnects forever. A cookie pins a browser
            // session to the kernel that issued its connectionId; only a fresh session (or a
            // failover) picks a new one via round-robin.
            .WithSessionAffinityConfig(new SessionAffinityConfig
            {
                Enabled = true,
                Policy = SessionAffinityConstants.Policies.Cookie,
                FailurePolicy = SessionAffinityConstants.FailurePolicies.Redistribute,
                AffinityKeyName = "Chronicle.Workbench.Affinity"
            });
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
