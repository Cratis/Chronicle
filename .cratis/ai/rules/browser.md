---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/browser.md -->

# Browser use

Browser access is a capability, not authority (see
[`capability-is-not-authority.md`](./capability-is-not-authority.md)). Every line is tagged
**[contract]** (binding) or **[convention]** (the house default) per the Three Levels of
Authority in [`general.md`](./general.md). Read this before any browser call.

## Permit and handoff

- **[contract] Production is read-only.** Write to a production site only when the user's
  request or an applicable policy names that exact action. A configured browser tool is not
  a permit.
- **[contract] One production tab per session.** No parallel polling or parallel findings on
  production. Run the health check first, close idle browsers, and look for stale tabs from
  other sessions before opening one.
- **[convention] Prefer staging or a local copy** for interactive and visual smoke runs.
- **[contract] Only the `browser-operator` agent receives browser tools.** Hand it the URL,
  scope, permit and artifact directory. The main session does not do browser work itself
  unless no such agent is available. No other agent gets browser tools.
- **[contract] Stop for a human on sign-in, 2FA or CAPTCHA.** Record
  `UI UNVERIFIED — needs human sign-in` and continue the other lanes. Do not block on it.

## Artifacts and uploads

- **[contract] Write artifacts and stage uploads only under `.ai-work/<task>/browser/`**
  (see [`local-work-artifacts.md`](./local-work-artifacts.md)). If a path guard blocks you,
  stop and report. Never monkey-patch DOM or file APIs to get around it.
- **[contract] No secrets or tokens in artifacts, URLs or logs.**

## Evidence

- **[contract] A UI verification records:** URL, build or version, screenshot path under
  `.ai-work/`, correlation id, timestamp and environment. Without these it is unverified.

## Headless production-smoke checklist

- **[convention]** Port-forward to the service.
- **[convention]** Front it with a GET-only proxy that injects the required headers.
- **[convention]** Drive it from a fresh headless browser with no persistent profile.
- **[convention]** Give any signed-in profile a single owner.
- **[convention]** Tear down the browser, proxy and port-forward when done.
