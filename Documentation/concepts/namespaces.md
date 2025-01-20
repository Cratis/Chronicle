# Namespaces

Chronicle has been designed with the concept of namespaces from the ground up.
Namespaces provide a way to provide segregation of data in the same event store.
This can typically be used for multi-tenant scenarios were you have multiple
tenants of your system and you want to provide data segregation between them.

When not specified, Chronicle will use the **Default** namespace.

> Note: A tenant is typically an organization or an organizational unit that uses a system.
> In cloud terminology it is often linked to Software as a Service offerings were companies
> can sign up to use a system. The system being the same for all these companies, or tenants,
> but all their data segregated from each other.

## Data Segregation

With Chronicle, all namespace specific data sits in its own database in the underlying
data storage. This helps us avoid data leakage between namespaces, or tenants. With this
you also get a better utilization of resources with mechanisms like indexing that happens
on a database level.
