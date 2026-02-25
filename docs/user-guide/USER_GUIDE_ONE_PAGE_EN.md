# EasySave - One Page User Guide (EN)

## 1. Launch

Inside the application folder:

- double-click `EasySave.exe`

Optional command mode:

```bat
EasySave.exe "1-3"
EasySave.exe "1;3;5"
```

## 2. Quick workflow

1. `New job`
2. Fill `Name`, `Source`, `Destination`, `Type`
3. `Save`
4. Select one or more jobs
5. `Run selection` or `Run all`
6. Control runtime using `Start`, `Pause`, `Cancel`

For an illustrated visual guide:

- `docs/user-guide/en/EasySave_GUI_User_Guide.html`
- `docs/user-guide/en/EasySave_GUI_User_Guide.pdf`

## 3. Backup types

- `Full`: copies all files
- `Differential`: copies missing or newer files only

## 4. Critical settings

| Setting | Values | Impact |
|---|---|---|
| Language | `en`, `fr` | UI language |
| Log format | `Json`, `Xml` | Log format |
| Log storage mode | `Local`, `Docker`, `Both` | Log destination |
| Business software to block | Process name | Auto-pause when process is running |
| File extensions to encrypt | free list separated by commas | CryptoSoft encryption |
| Priority file extensions | free list separated by commas | Cross-job priority |
| File size not to back up in parallel (KB) | integer >= 0 | Large-file serialization threshold |

Extensions note:

- plain text list, not strict CSV formatting
- example: `.txt, txt, .log`

## 5. Useful paths (Windows)

- Config: `%APPDATA%\EasySave\userdata\userconfig.json`
- Jobs: `%APPDATA%\EasySave\Jobs\jobs.json`
- Live state: `%APPDATA%\EasyLog\Progress\state.json`
- Local logs: `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml`
- CryptoSoft (user package): `Resources\CryptoSoft.exe`

## 6. Quick troubleshooting

- Encryption missing: verify `Resources\CryptoSoft.exe` + configured extensions.
- Jobs stuck paused: check business process + clear manual pause.
- No Docker logs: check `Docker/Both` mode + active log server.

## 7. Related documents

- Full guide EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- Full guide FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- One-page FR: `docs/user-guide/USER_GUIDE_ONE_PAGE.md`
- Debug guide: `docs/debug/DEBUG_GUIDE.md`
- Unit test guide: `docs/testing/UNIT_TESTS.md`
