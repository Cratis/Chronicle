# Scenarios

Sometimes the fastest way to learn a tool is to see it model a problem you already understand. Each
scenario here takes a familiar domain and walks through how you'd build it with Chronicle — the events
to capture, the projections that turn them into read models, and the reactions that follow. They're
problem-oriented and self-contained: pick the one closest to what you're building and adapt it.

| Scenario | What it models |
| -------- | ----------- |
| [Blog](./blog.md) | Posts and comments, with projections that build the read models a blog needs. |
| [E-commerce order](./ecommerce-order.md) | An order lifecycle — state changes over time, captured as events. |

Looking for the mechanics behind these? The [guides](/chronicle/projections/) cover projections,
reactors, and reducers in depth, and the [tutorial](/chronicle/tutorial/) builds one domain end to end.
