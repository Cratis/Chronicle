---
profile: cratis/engineering
---
<!-- cratis-ai-managed: rules/issue-assignment.md -->

# Claim the issue you start work on

When an agent starts work on a GitHub issue, the issue says who is on it. Nobody can
tell from an unassigned issue that work has begun, so two people (or two agents) pick it
up, or it looks abandoned while it is being done.

- **Applies when** the work is tied to an issue with a number in a known repository: the
  user named it (`#123`, `Cratis/Repo#123`, an issue URL), or the branch, task or handover
  names it. Work with no issue, or an issue whose repository you cannot determine, is
  outside this rule; never guess a number or a repository.
- **Read the assignees first**, in the issue's own repository:
  `gh issue view <n> --repo <owner>/<repo> --json assignees,state`.
- **Assign only when nobody is assigned.** Run
  `gh issue edit <n> --repo <owner>/<repo> --add-assignee @me` so the current `gh` user is
  the assignee. `@me` resolves to whoever is authenticated, so it is the user the agent
  works for, not a name you invent.
- **Never replace or add to an existing assignment.** Someone already on the issue means
  it is claimed; leave it, and mention it if it affects the work.
- **Do it once, at the start,** before the first edit. Do not re-check on every turn.
- **Skip a closed issue** and say so; do not reopen or assign it.
- **If assigning fails** (no permission, `gh` not authenticated, the issue is in a
  repository you cannot write to), say so in one line and continue with the work. A missing
  assignee is never a reason to stop.

The user's request to work on the issue is the authority for this assignment; do not ask
before doing it. It authorizes assigning the issue and nothing else: commenting, labeling
or closing still follow [pull-requests.md](./pull-requests.md) and the user's request.
