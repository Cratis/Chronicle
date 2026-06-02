#

We're building a user experience for creating named queries representing a configuration that can be saved in a structure.
The structure is basically

+- User queries (queries only for my user)
| |
| +- Folder
| |   +- Query
| |
+- Shared queries
| |
| +- Folder
| |   +- Query

## Histogram

- Add a histogram read model with a query on it in API/EventSequences - find a good name for the query
- Read model should hold EventSequenceNumber representing the lowest representation of the span of the entry, lowest Occurred value and a count, representing number of events in the time interval/span
- Expose histogram query from the gRPC services in Contracts -> Services that also has a implementation in EventSequence storage
- We need a way to specify resolution of the histogram (per hour, per day or something - not sure what is normal here)

## Command -> Events

Lets implement for the Kernel in the Server project in a namespace called Commands, handlers that can turn return values that
are either single events or multiple events into something that gets automatically appended:

/Volumes/Code/Cratis/Arc/Source/DotNET/Chronicle/Commands/SingleEventCommandResponseValueHandler.cs
/Volumes/Code/Cratis/Arc/Source/DotNET/Chronicle/Commands/EventsCommandResponseValueHandler.cs

They should recognize events that are system events - we have ways of doing this.

## Queries

Lets rename the SequencesFuture to Sequences.
Named queries are only created when they're actually saved.
In the Tab par with Queries - we want to have a + icon button - sticky to the left for creating queries.
When a query is not saved it should have an * besides it in the name and the Save button should light up.

Creating a folder should be clicking the + sign and then as it is now, a folder pops up, but we don't save
it until we hit enter.

We want to have commands and events for all of this, leveraging model bound commands.
Think:

- AddEventSequenceQueryFolder -> EventSequenceQueryFolderAdded (Public folders)
- AddEventSequenceQueryFolderForUser -> EventSequenceQueryFolderForUserAdded
- AddEventSequenceQuery -> EventSequenceQueryAdded (pointing to folder)

Ideally we would then have internal Projections - since we don't have the full client projection API in the core, lets
build it using the Projection Declaration Language (See the documentation).

Basically we want then a ReadModel for EventSequenceQueryFolder and then EventSequenceQuery - folder would then have
a property saying who owns it; the user id (CausedBy from event context) when it is the EventSequenceQueryFolderForUserAdded
event, and `System` (use constant) when it is EventSequenceQueryFolderAdded. This can then be used as filtering when
we get all folders and queries.

Lets keep the MVVM style, but clean it up - commands and queries should be taken as dependencies on the view models.

