# RGui

A cross-platform desktop GUI for [ripgrep](https://github.com/BurntSushi/ripgrep).

![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)

![Screenshot](.github/images/Search.png)

## Features

- Live-streaming results as ripgrep finds them
- Case-sensitive / regex toggles
- Double-click any result to open it in VS Code at the correct line

## Requirements

ripgrep is **bundled** with RGui — no separate install is needed. The published
binary ships the matching `rg` alongside it and runs that copy.

Building from source additionally requires [.NET 10](https://dotnet.microsoft.com/download).

## Development Setup

<details>
<summary>Click to see...</summary>

After cloning, activate the git hooks:

```bash
git config core.hooksPath .githooks
```

This enables the pre-commit format check (`dotnet format --verify-no-changes`).

## Build & Run

```bash
dotnet run --project src/
```

The bundled `rg` is copied from `vendor/rg/<rid>/` (see `vendor/rg/README.md`).
On publish this is the target runtime; on a plain `dotnet run` it falls back to
the host runtime, so development works as long as the matching binary has been
vendored.

## Tests

```bash
dotnet test
```

## Publish (single-file binary)

```bash
# Linux
dotnet publish src/ -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# macOS (Apple Silicon)
dotnet publish src/ -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Windows
dotnet publish src/ -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
Output lands in `bin/Release/net10.0/<rid>/publish/`.
</details>

## Acknowledgements

This project bundles and wraps [ripgrep](https://github.com/BurntSushi/ripgrep) by Andrew Gallant, which is dual-licensed under [The Unlicense](https://github.com/BurntSushi/ripgrep/blob/master/UNLICENSE) or the [MIT License](https://github.com/BurntSushi/ripgrep/blob/master/LICENSE-MIT). Its license terms permit redistribution of the binary.

## License

[MIT](LICENSE) © Christopher Ayre
