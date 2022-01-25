# Tenancy

Aksio Cratis has been designed with the concept of multi-tenancy from the ground up.

## What is a tenant

A tenant is typically an organization or an organizational unit that uses a system.
In cloud terminology it is often linked to Software as a Service offerings were companies
can sign up to use a system. The system being the same for all these companies, or tenants,
but all their data segregated from each other.

## Why multi tenancy

Rather than setting up a full unique environment per tenant, we want to share the expensive
resources and scale these according to traffic. Compute cycles are the most expensive in
any data-center, while storage is the cheapest. It makes sense from a monetary standpoint to
then optimize for this scenario. With microservices being at the heart of Aksio, we can
control density of the services in a fine-grained manner as well - packing the most compute
out of the virtual and physical machines they run on.

## Resources

When using resources such as a database. Rather than having one database holding all the
data for all the tenants of the system, we split this up into multiple databases.
This makes it easier for us to maintain data segregation and don't run the risk of data
leakage between tenants. It also removes the need for having to remember to include
tenant in queries, which can be error prone. We consider tenancy to be a cross cutting
concern and we want this to be applied as automatic and seamless as possible.

Resources such as the [event log](./clients/dotnet/events/events.md) and [mongodb](./application-model/mongodb.md)
comes pre-configured to be multi-tenant aware.

## How is a tenant resolved?

In order for any of the cross cutting mechanisms to work, we need to know what tenant we're
working with. This is identified through the existence of an **HTTP header** called `Tenant-ID`.
The identifier is in the form of a Guid and during development we tend to use what is known
as the **development tenant**, which has the value `3352d47d-c154-4457-b3fb-8a2efb725113`.

By using an extension such as [ModHeader](https://modheader.com/) to your browser, you can easily
set the tenant when running your microservice. Read more [here](https://modheader.com/guide/).
