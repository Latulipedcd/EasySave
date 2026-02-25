# EasySave User Guide (Full)

| Field | Value |
|---|---|
| Product | EasySave |
| Audience | End users (executable package) |
| Scope | `EasySave.exe` + `Resources` folder |
| Doc version | 2.1 |
| Last update | February 25, 2026 |

## 1. What your EasySave folder should contain

Your user package should include at least:

- `EasySave.exe`
- `Resources\Languages\`
- `Resources\CryptoSoft.exe`

This guide is written for that executable package (not for source repository usage).

## 2. Starting EasySave

## 2.1 Standard launch (GUI)

Double-click:

- `EasySave.exe`

The main window provides:

- jobs list
- action buttons (`New job`, `Edit job`, `Run selection`, `Run all`, `Delete selection`)
- job details panel
- `Settings` menu

## 2.2 Command launch (optional)

From a terminal opened in the application folder:

```bat
EasySave.exe "1-3"
EasySave.exe "1;3;5"
```

Accepted selection formats:

- `1-3`: range
- `1;3;5`: explicit IDs

## 3. User Workflow

1. Create a job (`New job`)
2. Fill name, source, destination, type
3. Save
4. Select one or more jobs
5. Run (`Run selection` or `Run all`)
6. Monitor and control (`Start`, `Pause`, `Cancel`)

For a visual illustrated walkthrough:

- EN HTML: `docs/user-guide/en/EasySave_GUI_User_Guide.html`
- EN PDF: `docs/user-guide/en/EasySave_GUI_User_Guide.pdf`

## 4. Job Management

## 4.1 Create

Required fields:

- `Name` (required, unique)
- `Source folder` (required)
- `Destination folder` (required)
- `Type` (`Full` or `Differential`)

## 4.2 Edit

- select exactly one job
- click `Edit job`
- modify values and save

## 4.3 Delete

- single or multi-selection
- confirm deletion

## 5. Backup Behavior

| Type | Rule |
|---|---|
| Full | Copies all files from source |
| Differential | Copies only files missing or newer than destination |

Execution:

- selected jobs run in parallel
- per-job runtime control is available
- live monitoring relies on `state.json`

Possible statuses:

- `Inactive`, `Active`, `Paused`, `Completed`, `Error`, `Cancelled`

## 6. Settings Reference

Settings are persisted in:

- `%APPDATA%\EasySave\userdata\userconfig.json`

| Setting | Values | Effect |
|---|---|---|
| Language | `en`, `fr` | UI language |
| Log format | `Json`, `Xml` | Log format |
| Log storage mode | `Local`, `Docker`, `Both` | Log destination |
| Business software to block | process name (without `.exe`) | Auto-pause when process is detected |
| File extensions to encrypt | free list separated by commas | CryptoSoft encryption on matching files |
| Priority file extensions | free list separated by commas | Priority gate across parallel jobs |
| File size not to back up in parallel (KB) | integer >= 0 | Files above threshold are serialized |

Important note about extensions:

- this is not strict CSV formatting
- this is plain text list separated by commas, for example: `.txt, txt, .log`
- case-insensitive matching

## 7. CryptoSoft (User Package)

Path to verify:

- `Resources\CryptoSoft.exe`

Behavior:

1. if file extension matches, EasySave attempts encryption
2. otherwise, normal copy
3. if CryptoSoft is missing, EasySave falls back to normal copy

## 8. Important Runtime Files

| Item | Default Windows path | Usage |
|---|---|---|
| User config | `%APPDATA%\EasySave\userdata\userconfig.json` | Persisted settings |
| Jobs | `%APPDATA%\EasySave\Jobs\jobs.json` | Job definitions |
| Live state | `%APPDATA%\EasyLog\Progress\state.json` | Execution monitoring |
| Local logs | `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml` | Execution history |

## 9. Quick Troubleshooting

## 9.1 Encryption not applied

Check:

1. `Resources\CryptoSoft.exe` exists
2. encryption extensions are configured
3. logs (`WorkType`, `EncryptionTimeMs`)

## 9.2 Jobs remain paused

Check:

1. monitored business process still running
2. manual pause not cleared
3. restart with `Start`

## 9.3 No Docker logs

Check:

1. storage mode is `Docker` or `Both`
2. log server is reachable on `127.0.0.1:11000`

## 10. Best Practices

- Keep job names short and explicit.
- Test on a small dataset before large runs.
- Start with `Local` logs, then use `Both` if Docker logging is needed.
- Enable priority extensions only when needed.

## 11. Related Documentation

- Full FR guide: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- One-page FR summary: `docs/user-guide/fr/USER_GUIDE_ONE_PAGE.md`
- One-page EN summary: `docs/user-guide/en/USER_GUIDE_ONE_PAGE_EN.md`
- Debug guide: `docs/debug/DEBUG_GUIDE.md`
- Unit test guide: `docs/testing/UNIT_TESTS.md`
