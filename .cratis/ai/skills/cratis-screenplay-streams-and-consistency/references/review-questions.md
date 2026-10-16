<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/review-questions.md -->
# Review questions

Questions to ask when reviewing streams and consistency.

**Review questions** (findings only with a domain consequence)
- What single question does this stream answer? Does it grow for the right reason?
- What happens when two people do this at the same moment? When the reply is lost?
- Is any stream a collection or a log wearing an instance's name (decision tree and red flags in
  `references/stream-patterns.md`)?
- Does an optional property cover two situations that the business names differently?
- Does a consumer in another module depend on an event we treat as internal?
- Could this calculated value matter historically (price, assessment)? Then it may be a fact.
- Is "eventually consistent" here a business choice, or an unexamined gap?
