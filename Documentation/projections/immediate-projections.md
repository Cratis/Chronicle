# Immediate Projections

> **Status**: This documentation is in progress and will be updated soon.

Immediate projections allow you to work with projections that provide strong consistency by processing events synchronously during the event append operation.

## Overview

While most projections in Chronicle operate with eventual consistency, immediate projections process events synchronously, ensuring that the read model is updated before the event append operation completes.

## When to Use Immediate Projections

Immediate projections are useful when:

- You need strong consistency guarantees
- The read model must be immediately available after an event
- You're building critical business flows that require synchronous state updates

## Performance Considerations

Because immediate projections run synchronously, they can impact the performance of your event append operations. Use them judiciously and only when strong consistency is required.

## See Also

- [Eventual Consistency](eventual-consistency.md) - Learn about eventual consistency in projections
- [Model-Bound Projections](model-bound/index.md) - How to work with model-bound projections
