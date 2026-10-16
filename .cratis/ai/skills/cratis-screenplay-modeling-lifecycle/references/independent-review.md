<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/independent-review.md -->
# Independent review (P5)

How the P5 review is staffed and recorded.

P5 needs a review in a fresh context by an agent that authored nothing in scope. It is stronger
when the reviewer runs on a different model family than every author; choose that in your
harness's own agent or model settings, never in the corpus. Each packet records the model the
agent ran on when the harness exposes it, else `model: not exposed`. A same-model review is
labelled as such. The user decides whether it is enough
(`rules/capability-is-not-authority.md`); it is never blocked as a matter of authority.
