---
title: Test coverage statistics
description: Test coverage statistics for the Chronicle project, with the key coverage metrics tracked over time in an embedded report.
---

<!-- markdownlint-disable MD033 -->

This section contains test coverage statistics for the Chronicle project, tracking key metrics over time.

The measured date and commit identify the last usable coverage report, not the release publication time. A missing or incomplete report, including one without a trusted measurement timestamp, leaves that measurement unchanged. The report shows missing coverage when a newer attempt has no usable report, or stale coverage when the measurement is more than seven days old.

New nightly measurements come from the successful scheduled `main` coverage merge job, not a release cache. Discovery checks at most 30 runs within 30 days. A successful all-skipped run is not a producer. A selected feed that fails validation does not fall back to older coverage. Re-observing the same feed changes only the publish observation, not its measurement date or history.

The publish run and requested version are **observer labels**, with release context unverified. Publication absence or timing does not reject valid coverage; even equal source and publish commits do not establish a measured release version. The feed covers only the existing merge job's participating .NET specs/integration reports, not every project or provider.

The artifact, checksums and provenance are unsigned. API identity, digest, archive, timestamp and source checks validate transport, not test correctness or independent attestation. Isolation of the external producer cache from PR-controlled inputs remains unverified and is required before trusted-feed acceptance. The PAT-backed detached-HEAD documentation writer retains its existing credential/trigger boundary; its real write authority and documentation-trigger behavior still require a separately authorized workflow proof. Offline fixtures do not establish either boundary.

<style>
  .coverage-iframe-container {
    width: 100%;
    height: 800px;
    border: none;
    margin: 0;
    padding: 0;
  }
  .coverage-iframe {
    width: 100%;
    height: 100%;
    border: none;
  }
</style>

<div class="coverage-iframe-container">
  <iframe src="coverage.html" class="coverage-iframe" title="Test Coverage Statistics"></iframe>
</div>
