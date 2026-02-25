# EasySave

EasySave is a .NET 10 backup application with:

- a desktop GUI (Avalonia)
- command-line execution mode
- local and/or Docker log routing
- runtime controls (pause, resume, cancel) for parallel jobs

## Documentation

- Full user guide (EN, Markdown): [docs/user-guide/en/EasySave_User_Guide_Full.md](docs/user-guide/en/EasySave_User_Guide_Full.md)
- Guide utilisateur complet (FR, Markdown): [docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md](docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md)
- One-page summary (FR): [docs/user-guide/USER_GUIDE_ONE_PAGE.md](docs/user-guide/USER_GUIDE_ONE_PAGE.md)
- Guide de debug (FR): [docs/debug/DEBUG_GUIDE.md](docs/debug/DEBUG_GUIDE.md)
- Guide des tests unitaires (FR): [docs/testing/UNIT_TESTS.md](docs/testing/UNIT_TESTS.md)
- Legacy printable guides (HTML/PDF):
  - EN HTML: [docs/user-guide/en/EasySave_GUI_User_Guide.html](docs/user-guide/en/EasySave_GUI_User_Guide.html)
  - EN PDF: [docs/user-guide/en/EasySave_GUI_User_Guide.pdf](docs/user-guide/en/EasySave_GUI_User_Guide.pdf)
  - FR HTML: [docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.html](docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.html)
  - FR PDF: [docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.pdf](docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.pdf)

## UML and Diagrams

### Current (V2)

- Activity diagram: [docs/UML/V2/Activity Diagram EasySave V2.jpg](docs/UML/V2/Activity%20Diagram%20EasySave%20V2.jpg)
- Class diagram: [docs/UML/V2/Class_V2.jpg](docs/UML/V2/Class_V2.jpg)
- Sequence diagram: [docs/UML/V2/SequenceV2.png](docs/UML/V2/SequenceV2.png)
- Use-case diagram: [docs/UML/V2/Use_Case_Diagram_for_EasySave_v2.0.jpg](docs/UML/V2/Use_Case_Diagram_for_EasySave_v2.0.jpg)
- VPP sources:
  - [docs/UML/V2/Activity-Diagram-V2.0.vpp](docs/UML/V2/Activity-Diagram-V2.0.vpp)
  - [docs/UML/V2/CLass_V2.vpp](docs/UML/V2/CLass_V2.vpp)
  - [docs/UML/V2/Use-Case-V2-0.vpp](docs/UML/V2/Use-Case-V2-0.vpp)

### Archives

- V1.1: [docs/UML/V1.1](docs/UML/V1.1)
- V1: [docs/UML/V1](docs/UML/V1)

## Key Features

- Create, update, delete, reorder backup jobs
- Full and differential backup modes
- Execute selected jobs or all jobs in parallel
- Job runtime controls: pause/resume/cancel
- Cross-job priority extension rule
- Large-file parallelism threshold
- Business software process monitoring (auto-pause while process is running)
- Extension-based CryptoSoft encryption
- FR/EN UI and persisted settings
- Log formats: JSON or XML
- Log storage modes: Local, Docker, Both

## Requirements

- .NET SDK 10.x
- Windows recommended (especially for CLI auto-install behavior)

Check SDK:

```bash
dotnet --version
```

## Build and Run

Build all projects:

```bash
dotnet build EasySave.slnx -c Release
```

Run GUI:

```bash
dotnet run --project src/GUI/GUI.csproj
```

Run console interactive mode:

```bash
dotnet run --project src/Console/Console.csproj
```

Run command mode (single argument):

```bash
dotnet run --project src/GUI/GUI.csproj -- "1-3"
dotnet run --project src/GUI/GUI.csproj -- "1;3"
```

## CLI command (`EasySave`)

On Windows, EasySave attempts to install `EasySave.cmd` automatically at startup:

- target directory: `%LOCALAPPDATA%\EasySave\bin`
- user PATH is updated if needed

Manual scripts:

```bat
scripts\install-easysave-cli.cmd
scripts\uninstall-easysave-cli.cmd
```

## Runtime Files and Paths

- Jobs: `%APPDATA%\EasySave\Jobs\jobs.json`
- User config: `%APPDATA%\EasySave\userdata\userconfig.json`
- Progress state: `%APPDATA%\EasyLog\Progress\state.json`
- Local logs: `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml`

For debugging and Docker logging details, see [docs/debug/DEBUG_GUIDE.md](docs/debug/DEBUG_GUIDE.md).

## Run Unit Tests

```bash
dotnet test EasySave.slnx
```

Coverage (collector):

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj --collect:"XPlat Code Coverage"
```

More details: [docs/testing/UNIT_TESTS.md](docs/testing/UNIT_TESTS.md).
