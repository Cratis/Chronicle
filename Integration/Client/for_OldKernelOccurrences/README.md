# Released-kernel occurrence compatibility

This namespace is **opt-in**. Without `CHRONICLE_OLD_KERNEL_SPECS=1` its facts are reported as skipped (not passed, not failed) and the fixture starts no container, because the ordinary CI integration artifact is not release-stamped. Build and run it with the variable and the release version stamp, including all project references:

```sh
CHRONICLE_OLD_KERNEL_SPECS=1 TEST_MAX_ATTEMPTS=1 .github/scripts/run-tests-with-retry.sh \
  Integration/Client/Client.csproj \
  FullyQualifiedName~Cratis.Chronicle.Integration.for_OldKernelOccurrences \
  -p:Version=19.74.0 -p:DisableDockerBuild=true -p:DisableProxyGenerator=true
```

Run through the normal bounded integration runner with Docker available. Do not use `--no-build` unless the client **and Contracts/Connections assemblies** were built with this stamp. Stamping only the integration assembly does not change the SDK's identity.

`ChronicleClientIdentity` reads the Connections assembly's informational version. `Contracts.ProtocolVersion.Current` reads the Contracts assembly's informational version. The publish workflow supplies both through MSBuild's `Version` property. Unversioned local builds default to 1.0.0, which is not a 19.x peer and reverses the released server's contract comparison. Compatibility specs cover newer-client/older-kernel comparison within the same major. This fixture uses the same legitimate release stamping, not a compatibility-check bypass or a mutable version override.

The default image is `cratis/chronicle:19.68.0-development`, the latest release before either occurrences capability. Override it with `CHRONICLE_OLD_KERNEL_IMAGE`; the fixture verifies the advertised capabilities really are absent. This is deliberately separate from `CRATIS_CHRONICLE_LOCAL_IMAGE`, which the ordinary integration build can overwrite with current source.
