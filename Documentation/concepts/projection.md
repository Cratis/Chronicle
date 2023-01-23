# Projection

A projection in Cratis is a specialized observer. It is a declarative approach to describing
events you're interested in and how you want Cratis to project it into something that you can
display in your application or provide as data for an API. Every projection can be very specialized
for the purpose it is being used in, in fact it is encouraged to create specialized projections
rather than reuse a projection for multiple scenarios. The reason for this is that you then don't
run the risk of serving multiple conflicting scenarios, which could for instance impact performance
and also general maintainability of your code.

Cratis projections support relationships, one-to-one and one-to-many. It also supports functions that
allow you to count, add or subtract values. The identifier of the projected document can also be
configured. By default it will use the [event source identifier](./event-source.md), but you can
point it to any other property or create a composite of multiple properties.

Projections support using the metadata properties of an event as well. This lets you create
composite keys that could for instance leverage the `occurred` date/time value and effectively
create group by scenarios. This would effectively give you an easy way to do simple histograms,
with count, add and/or subtract of values in the histogram.
