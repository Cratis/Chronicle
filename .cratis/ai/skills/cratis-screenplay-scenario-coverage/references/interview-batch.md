<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/interview-batch.md -->
# Interview batch for scenario coverage

The scenario-coverage interview questions.

1. **Depth.** Reference examples only, every command variation, or production-grade including
   edge cases? Production work needs the full matrix. Follow-up: which commands matter most?
2. **Known rules and edge cases.** Which mistakes would be costly? For each rule: one allowed
   request, the nearest refused one, and why they differ (`references/scenario-catalogue.md`
   "Eliciting cases").
3. **Use.** Design record, executable (V2/V3), reference run (V4) or rendered tests (V5)? It
   decides the admitted forms (`references/spec-forms-by-mode.md`).
4. **Validation.** Who confirms the business outcomes: domain expert, developer, tester? For a
   group session use `references/scenario-workshop.md`.
**Unattended:** assume visibly. Write each assumption as a `question` cell with the default you
used; never invent a rule. Record who actually answered; an agent asking itself four kinds of
question is not four approvals.
