# Event Metadata Tags

Event metadata tags provide a structured way to categorize and identify events within the Chronicle system. Below are the formalized tags available:

## EventSourceType

- **Description**: Overarching, binding concept (e.g., Account).

## EventSourceId

- **Description**: Unique identifier for the event source.

## EventStreamType

- **Description**: A concrete process related to event source type (e.g., Onboarding, Transactions). Automatically linked to an AggregateRoot in Chronicle.

## EventStreamId

- **Description**: A marker that can be used to separate independent streams for a stream type (e.g., Monthly, Yearly).

You can declare the event source type and the stream types once, as an event source definition. See [Event sources and streams](../event-sources/index.mdx). All four values can narrow a concurrency scope, which is how Chronicle expresses an append condition for a [Dynamic Consistency Boundary](../dynamic-consistency-boundary/chronicle.md). Custom tags cannot.
