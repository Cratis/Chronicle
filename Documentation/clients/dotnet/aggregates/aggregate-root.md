# Aggregate Root

The concept of an Aggregate Root comes from [Domain Driven Design](https://martinfowler.com/bliki/DDD_Aggregate.html).
Its role is to govern the interaction of domain objects that should be treated as a single unit.
With event sourcing, an aggregate root typically is responsible for applying events as it sees fit according
to its domain logic and rules.

Said in another way, Aggregate Root objects is responsible for managing the domain transaction.

