# Constraints

To keep integrity within typically an [event source](./event-source.md) when appending events to an [event sequence](./event-sequence.md),
you can leverage constraints. Constraints are rules that run on a database level within Chronicle, allowing or not allowing an event to be
appended. This is one of the main ways Chronicle enforces the [Dynamic Consistency Boundary](../dynamic-consistency-boundary/chronicle.md)
by validating that the decision remains correct at append time.

## Unique Constraint

A **unique constraint** specifies events that work on a specific value you want to keep unique within an event source.
For instance, lets say you're creating a system for registering users, the user name is typically something you want to keep a unique
constraint on. Any events that either create the user or modify the user name in any way would typically then be included in the
constraint definition.

## Unique Event Type Constraint

The **unique event type constraint** lets you constrain on a specific event type being unique per event source. If any attempts of
appending the same event type twice for an event source is done, it will not be a constraint violation.
