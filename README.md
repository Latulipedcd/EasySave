# EasySave

EasySave is a backup application built in C# with .NET.
This repository currently contains the Console architecture (Core + Application + Console + Log) and the base for future UI versions.

## Features

- Create, update, list and delete backup jobs
- Execute one or many jobs by index
- Command formats:
  - `1-3` for a range
  - `1;3` for explicit selection
  - `1 3` as shell-friendly alternative
- FR/EN language support
- JSON log and progress state output
- CLI command support (`EasySave ...`)

## Tech Stack

- C# / .NET 10
- Layered architecture:
  - `Core`: models, services, business logic
  - `Application`: view models and language/config management
  - `Console`: terminal UI and CLI entry point
  - `Log`: log abstractions and writers

## Project Structure

```text
src/
  Application/
  Console/
  Core/
  Log/
scripts/
  install-easysave-cli.cmd
  uninstall-easysave-cli.cmd
EasySave.slnx
```

## Prerequisites

- Windows (recommended for CLI auto-install behavior)
- .NET SDK 10.x

Check SDK:

```bash
dotnet --version
```

## Build and Run

Build:

```bash
dotnet build src/Console/Console.csproj -c Release
```

Run interactive mode:

```bash
dotnet run --project src/Console/Console.csproj
```

Run command mode:

```bash
dotnet run --project src/Console/Console.csproj -- 1-3
dotnet run --project src/Console/Console.csproj -- "1;3"
dotnet run --project src/Console/Console.csproj -- 1 3
```

## Use EasySave From Anywhere

When the app starts, it tries to install a command shim automatically on Windows:

- creates `EasySave.cmd` in `%LOCALAPPDATA%\EasySave\bin`
- adds this directory to the user `PATH` (if missing)

After first run, open a new terminal and use:

```bash
EasySave 1-3
EasySave "1;3"
EasySave 1 3
```

You can also install/uninstall manually:

```bat
scripts\install-easysave-cli.cmd
scripts\uninstall-easysave-cli.cmd
```

## Notes for PowerShell and Bash

- In PowerShell, `;` is a command separator, so prefer:
  - `EasySave "1;3"`
  - or `EasySave 1 3`
- `EasySave 1-3` works directly.

## Logs and State Files

At runtime, EasySave writes:

- backup logs (JSON)
- progress state file (JSON)

Paths depend on environment and user profile configuration.
