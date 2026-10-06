// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.EventStores;

using context = Cratis.Chronicle.Integration.Api.for_AlertIncidentQueries.when_querying_open_incidents.context;

namespace Cratis.Chronicle.Integration.Api.for_AlertIncidentQueries;

[Collection(ChronicleCollection.Name)]
public class when_querying_open_incidents(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public bool PageSucceeded;
        public bool LookupSucceeded;
        public bool CountsSucceeded;
        public bool MissingScopeRejected;
        public bool AllQueriesRequireAuthentication;

        async Task Establish()
        {
            var result = await Client.ExecuteCommand("/api/event-stores/ensure-event-store", new EnsureEventStore("unaffected"));
            result!.IsSuccess.ShouldBeTrue();
        }

        async Task Because()
        {
            var id = Guid.NewGuid();
            var page = await Client.ExecuteQuery<AlertIncidentPage>(
                $"/api/alerts/get-open-incidents?eventStore=unaffected&namespace=Default&limit=1&minimumSeverity=1&afterRaisedSequenceNumber=0&afterIncidentId={id}");
            var lookup = await Client.ExecuteQuery<AlertIncidentLookup>(
                $"/api/alerts/get-open-incident?eventStore=unaffected&namespace=Default&incidentId={id}");
            var counts = await Client.ExecuteQuery<AlertIncidentSummary>(
                "/api/alerts/get-open-incident-counts?eventStore=unaffected&namespace=Default");
            PageSucceeded = page?.IsSuccess == true;
            LookupSucceeded = lookup?.IsSuccess == true;
            CountsSucceeded = counts?.IsSuccess == true;
            var missing = await Client.ExecuteQuery<AlertIncidentPage>("/api/alerts/get-open-incidents");
            MissingScopeRejected = missing?.IsSuccess != true;
            Client.DefaultRequestHeaders.Authorization = null;
            var statuses = new List<HttpStatusCode>();
            foreach (var route in new[] { "get-open-incidents", $"get-open-incident?incidentId={id}", "get-open-incident-counts" })
            {
                using var response = await Client.GetAsync($"/api/alerts/{route}");
                statuses.Add(response.StatusCode);
            }
            AllQueriesRequireAuthentication = statuses.TrueForAll(status => status == HttpStatusCode.Unauthorized);
        }
    }

    [Fact] void should_bind_page_filters_and_paired_cursor() => Context.PageSucceeded.ShouldBeTrue();
    [Fact] void should_bind_scoped_lookup() => Context.LookupSucceeded.ShouldBeTrue();
    [Fact] void should_bind_scoped_counts() => Context.CountsSucceeded.ShouldBeTrue();
    [Fact] void should_reject_missing_required_store() => Context.MissingScopeRejected.ShouldBeTrue();
    [Fact] void should_require_authentication_for_all_queries() => Context.AllQueriesRequireAuthentication.ShouldBeTrue();
}
