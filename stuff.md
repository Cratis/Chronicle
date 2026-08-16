# Stuff To do

Lets continue work on https://github.com/Cratis/Chronicle/pull/3452.
You need to rebase on origin/main.

Also, look at taking in the general AI instructions (not the project specific) from Studio (/Volumes/Code/Cratis/Studio)
in case ours is out of sync.

The goal of this PR is to improve the user experience in general.
But for the Query side of things especially. You should be able to create
queries that are persisted for you as a user or for everyone.

The queries should have filtering capabilities as described. We also want
to have a type of histogram filter for time range picking that is part of this.
Look at how we've done things with the PivotViewer used, the source for it is found
in the components library (/Volumes/Code/Cratis/Components).

The filters should be done consistently with it. So basically a drop down of things to filter on
and that is what we persist with the queries. You should be able to reuse the filter component
from @cratis/components from the PivotViewer. If not, fix the @cratis/components library so
that it can be reused.

You can have multiple queries open at a time and you should also be able to sort the result,
which will also be persisted with the query.

Do not build anything using MVVM, just use React and leverage whatever we have of tools
in Arc and the Components library.

We don't need a Save button, the query should just save itself as you change things.
We do not need to use events and projections or anything like that for the queries.
Just a gRPC Service exposed through the REST APIs.

Favor using Arcs model bound commands and queries.

Expecting obviously specs and all.

There should be a button to perform the query in case something has changed.

In addition, you can bring in the following PR into this
https://github.com/Cratis/Chronicle/pull/2791

It should have a developer page in the sidebar, but only accessible when
running in development mode. The backend should not be even compiled in
for the non-development image, like we do with other things.
