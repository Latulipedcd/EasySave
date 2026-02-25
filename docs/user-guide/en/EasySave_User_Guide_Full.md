# EasySave User Guide (Full)

| Field | Value |
|---|---|
| Product | EasySave |
| Scope | GUI + command mode |
| Doc version | 2.0 |
| Last update | February 25, 2026 |

## 1. Purpose

EasySave is a backup application that supports:

- job management (create, edit, delete, reorder)
- parallel execution of one or more jobs
- live execution monitoring
- runtime controls (`Start`, `Pause`, `Cancel`)
- advanced rules (priority extensions, large-file threshold)
- extension-based CryptoSoft encryption
- local, Docker, or dual log routing

## 2. Getting Started

## 2.1 Requirements

- .NET SDK 10.x
- Windows recommended

Check:

```bash
dotnet --version
```

## 2.2 Launching

GUI:

```bash
dotnet run --project src/GUI/GUI.csproj
```

Console interactive mode:

```bash
dotnet run --project src/Console/Console.csproj
```

Command mode (single argument):

```bash
dotnet run --project src/GUI/GUI.csproj -- "1-3"
dotnet run --project src/GUI/GUI.csproj -- "1;3;5"
```

Accepted selection formats:

- `1-3` for ranges
- `1;3;5` for explicit job IDs

## 3. GUI Overview

## 3.1 Main window

- Left side: jobs list and global actions
- Right side: selected job details and runtime actions
- Top area: `Settings` menu
- Assistant widget: contextual runtime feedback

## 3.2 Main actions

- `New job`
- `Edit job` (single selection)
- `Run selection`
- `Run all`
- `Delete selection`

Jobs can be reordered by drag-and-drop.

## 3.3 Run-all monitor window

When `Run all` starts:

- a dedicated monitor window opens
- global and per-job progress are displayed
- per-job actions are available (`Start`, `Pause`, `Cancel`)
- close requires confirmation and can cancel all running jobs

## 4. Job Lifecycle

## 4.1 Create

Required fields:

- `Name`
- `Source folder`
- `Destination folder`
- `Type` (`Full` or `Differential`)

Validation:

- non-empty name
- unique name

## 4.2 Edit

- select exactly one job
- click `Edit job`
- update values and save

## 4.3 Delete

- select one or multiple jobs
- confirm deletion
- job list refreshes automatically

## 5. Backup Behavior

## 5.1 Backup types

| Type | Rule |
|---|---|
| Full | Copies all source files to destination |
| Differential | Copies only files missing at destination or newer than destination copies |

## 5.2 Parallel execution

- selected jobs run concurrently
- each job can be controlled independently
- live status is backed by `state.json`

## 5.3 Job statuses

| Status | Meaning |
|---|---|
| Inactive | Not started |
| Active | Running |
| Paused | Suspended (manual or business software) |
| Completed | Finished successfully |
| Error | Failed during execution |
| Cancelled | Stopped by user action |

## 5.4 Pause/Resume semantics

A job can be paused by:

- manual action
- business software monitoring

A paused job resumes only when both pause causes are cleared.

## 6. Settings (Complete Reference)

Settings are persisted in:

- `%APPDATA%\EasySave\userdata\userconfig.json`

| GUI setting | Values | Default | Effect |
|---|---|---|---|
| Language | `en`, `fr` | `en` | UI localization |
| Log format | `Json`, `Xml` | `Json` | Log encoding format |
| Log storage mode | `Local`, `Docker`, `Both` | `Local` | Log destination |
| Business software to block | Process name (without `.exe`) | empty | Pauses jobs while process is running |
| File extensions to encrypt | CSV list (`.txt, .pdf`) | empty | Matching files go through CryptoSoft |
| Priority file extensions | CSV list (`.sql, .docx`) | empty | Cross-job priority gate for these extensions |
| File size not to back up in parallel (KB) | Integer >= 0 | `0` | Files above threshold are serialized (one at a time) |

Notes:

- extension matching is case-insensitive
- leading dot is optional (`txt` becomes `.txt`)
- threshold `0` disables the large-file rule

## 7. CryptoSoft Encryption

## 7.1 Flow

For each file:

1. extension is checked against encryption extension list
2. if matched, EasySave attempts CryptoSoft execution
3. otherwise, standard copy is used

## 7.2 Locations

Repository source:

- `src/Application/Resources/CryptoSoft.exe`

Expected runtime location:

- `<application output folder>\Resources\CryptoSoft.exe`

## 7.3 Fallback behavior

If `CryptoSoft.exe` is missing:

- EasySave falls back to plain copy
- backup execution continues

## 8. Logs, State, and Persistent Files

| Item | Default Windows path | Usage |
|---|---|---|
| Jobs | `%APPDATA%\EasySave\Jobs\jobs.json` | Job definitions |
| User config | `%APPDATA%\EasySave\userdata\userconfig.json` | Saved settings |
| Live state | `%APPDATA%\EasyLog\Progress\state.json` | Real-time monitoring |
| Local logs | `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml` | Execution trace |

## 8.1 Key log fields

- `BackupName`
- `Source`, `Target`
- `WorkType` (`file_transfer`, `folder_creation`, `encryption`)
- `FileSize`
- `Duration`
- `EncryptionTimeMs`
- `ErrorMessage`
- `UserName`

## 8.2 Log storage modes

| Mode | Behavior |
|---|---|
| Local | Writes local file logs only |
| Docker | Sends logs to TCP log server only |
| Both | Writes local logs and sends Docker logs |

## 9. Docker Logging Mode

EasySave Docker logging target:

- `127.0.0.1:11000`

Start log server:

```bash
dotnet run --project LogServer/LogServer.csproj
```

If mode is `Docker` only and server is down:

- no local logs are produced
- traceability is reduced

## 10. Global `EasySave` Command

On Windows startup, EasySave attempts to install a CLI shim command.

Manual scripts:

```bat
scripts\install-easysave-cli.cmd
scripts\uninstall-easysave-cli.cmd
```

If global command is unavailable, use `dotnet run --project ... -- "<selection>"`.

## 11. Troubleshooting Runbook

## 11.1 Encryption not applied

Check:

1. encryption extensions configured
2. runtime presence of `Resources\CryptoSoft.exe`
3. logs (`WorkType`, `EncryptionTimeMs`)

## 11.2 Jobs remain paused

Check:

1. monitored business process still running
2. manual pause not cleared
3. `Start` action retried after condition removal

## 11.3 No Docker logs

Check:

1. storage mode is `Docker` or `Both`
2. `LogServer` is running
3. port `11000` is listening

## 11.4 Source not found errors

Check:

1. source folder exists at execution time
2. read permissions are valid
3. corresponding error log entry

## 12. Best Practices

- Use stable, explicit job names.
- Start with `Local` log mode, then switch to `Both` for deployment.
- Configure priority extensions only when clearly needed.
- Keep `MaxParallelFileSizeKb = 0` unless memory pressure requires throttling.
- Validate new rules on a small dataset first.

## 13. Related Documentation

- Full FR guide: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- One-page summary: `docs/user-guide/USER_GUIDE_ONE_PAGE.md`
- Debug guide: `docs/debug/DEBUG_GUIDE.md`
- Unit tests guide: `docs/testing/UNIT_TESTS.md`
