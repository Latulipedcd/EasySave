# EasySave - Guide Synthese (1 page)

## 1. Lancement

```bash
dotnet build EasySave.slnx -c Release
dotnet run --project src/GUI/GUI.csproj
```

Mode commande (argument unique):

```bash
dotnet run --project src/GUI/GUI.csproj -- "1-3"
dotnet run --project src/GUI/GUI.csproj -- "1;3;5"
```

## 2. Workflow express

1. `New job`
2. Renseigner `Name`, `Source`, `Destination`, `Type`
3. `Save`
4. Selectionner des jobs
5. `Run selection` ou `Run all`
6. Piloter l'execution avec `Start`, `Pause`, `Cancel`

## 3. Types de sauvegarde

- `Full`: copie tous les fichiers
- `Differential`: copie uniquement les fichiers absents ou plus recents

## 4. Parametres critiques

| Parametre | Valeurs | Impact |
|---|---|---|
| Language | `en`, `fr` | Langue UI |
| Log format | `Json`, `Xml` | Format de log |
| Log storage mode | `Local`, `Docker`, `Both` | Destination des logs |
| Business software to block | Nom de processus | Pause automatique si processus actif |
| File extensions to encrypt | CSV | Fichiers chiffres via CryptoSoft |
| Priority file extensions | CSV | Priorite inter-jobs |
| File size not to back up in parallel (KB) | Entier >= 0 | Seuil de serialisation des gros fichiers |

## 5. Chemins a connaitre (Windows)

- Jobs: `%APPDATA%\EasySave\Jobs\jobs.json`
- Config: `%APPDATA%\EasySave\userdata\userconfig.json`
- Etat live: `%APPDATA%\EasyLog\Progress\state.json`
- Logs locaux: `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml`
- CryptoSoft source: `src/Application/Resources/CryptoSoft.exe`
- CryptoSoft runtime attendu: `<output>\Resources\CryptoSoft.exe`

## 6. Depannage express

- Chiffrement absent: verifier extensions + presence runtime de CryptoSoft.
- Pas de logs Docker: verifier mode `Docker/Both` + serveur `LogServer`.
- Jobs bloques en pause: verifier logiciel metier + lever la pause manuelle.

## 7. Commandes utiles

Lancer serveur de logs:

```bash
dotnet run --project LogServer/LogServer.csproj
```

Lancer tests unitaires:

```bash
dotnet test EasySave.slnx
```

## 8. Documentation complete

- Guide complet FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- Full guide EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- Guide debug: `docs/debug/DEBUG_GUIDE.md`
- Guide tests unitaires: `docs/testing/UNIT_TESTS.md`
