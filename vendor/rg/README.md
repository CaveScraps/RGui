# Vendored ripgrep binaries

RGui bundles the `rg` executable and runs that copy (never one from `PATH`).
The build copies the binary matching the effective runtime next to the output
executable — the publish `RuntimeIdentifier`, or the host runtime for a plain
`dotnet run`/build. See the `<Content>` items in `src/RGui.csproj`.

## Layout

Drop the official release binaries here, one per supported runtime:

```
vendor/rg/win-x64/rg.exe
vendor/rg/linux-x64/rg
vendor/rg/osx-arm64/rg
```

On Unix make sure the executable bit is set (`chmod +x rg`) and committed —
otherwise the published copy won't run.

## Where to get them

Use the **official release archives**:
https://github.com/BurntSushi/ripgrep/releases

Record the version and SHA-256 of each binary you vendor so provenance is
auditable.

## Licensing

ripgrep is dual-licensed **"The Unlicense OR MIT"** — bundling and
redistribution are explicitly permitted. Keep a copy of the upstream license
text alongside the binaries (e.g. `vendor/rg/LICENSE`) to make the terms
unambiguous.
