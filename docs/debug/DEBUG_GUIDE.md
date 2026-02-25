# EasySave - Guide de Debug

| Champ | Valeur |
|---|---|
| Portee | Diagnostic runtime GUI/CLI |
| Version doc | 2.0 |
| Derniere mise a jour | 25/02/2026 |

## 1. Objectif

Ce guide fournit une procedure de diagnostic operationnelle pour:

- chiffrement CryptoSoft
- lecture des logs et de l'etat live
- mode Docker des logs
- pauses inattendues des jobs
- comportements lies aux nouveaux parametres (priorites, seuil gros fichiers)

## 2. Checklist de triage (2 minutes)

1. Identifier le mode d'execution (GUI, console, commande).
2. Capturer `userconfig.json`.
3. Verifier `state.json` pendant le run.
4. Verifier la presence de logs (local et/ou Docker).
5. Verifier la presence runtime de `CryptoSoft.exe`.

## 3. Emplacement de CryptoSoft

## 3.1 Source dans le depot

- `src/Application/Resources/CryptoSoft.exe`

## 3.2 Chemin attendu au runtime

EasySave cherche CryptoSoft ici:

- `<AppDomain.BaseDirectory>\Resources\CryptoSoft.exe`

Exemples:

- `src/GUI/bin/Debug/net10.0/Resources/CryptoSoft.exe`
- `<dossier publication>\Resources\CryptoSoft.exe`

## 3.3 Commandes de verification

Lister les copies de CryptoSoft:

```powershell
Get-ChildItem -Path . -Recurse -Filter CryptoSoft.exe | Select-Object FullName
```

Verifier la copie runtime depuis le dossier courant:

```powershell
Test-Path .\Resources\CryptoSoft.exe
```

## 3.4 Comportement si absent

Si CryptoSoft est absent:

- pas de crash bloqueur
- fallback en copie standard
- le job continue

## 4. Consultation des logs

## 4.1 Chemins standards (Windows)

| Element | Chemin |
|---|---|
| Configuration | `%APPDATA%\EasySave\userdata\userconfig.json` |
| Jobs | `%APPDATA%\EasySave\Jobs\jobs.json` |
| Etat live | `%APPDATA%\EasyLog\Progress\state.json` |
| Logs locaux | `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json` ou `.xml` |

## 4.2 Lire l'etat live

```powershell
Get-Content "$env:APPDATA\EasyLog\Progress\state.json" -Wait
```

Utilite:

- statut courant (`Active`, `Paused`, etc.)
- progression
- fichier en cours
- tailles restantes

## 4.3 Lire les logs locaux du jour

JSON:

```powershell
Get-Content "$env:APPDATA\EasyLog\Logs\log-$(Get-Date -Format yyyy-MM-dd).json"
```

XML:

```powershell
Get-Content "$env:APPDATA\EasyLog\Logs\log-$(Get-Date -Format yyyy-MM-dd).xml"
```

## 4.4 Champs prioritaires a verifier

- `BackupName`
- `Source`, `Target`
- `WorkType` (`file_transfer`, `folder_creation`, `encryption`)
- `FileSize`
- `Duration`
- `EncryptionTimeMs`
- `ErrorMessage`
- `UserName`

## 5. Debug du mode Docker

## 5.1 Rappel

Le logger Docker envoie vers:

- `127.0.0.1:11000`

## 5.2 Demarrage serveur de logs

```bash
dotnet run --project LogServer/LogServer.csproj
```

## 5.3 Verification port

```powershell
Get-NetTCPConnection -LocalPort 11000 -State Listen
```

## 5.4 Matrice de comportement

| Storage mode | Resultat attendu |
|---|---|
| `Local` | logs fichier uniquement |
| `Docker` | envoi TCP uniquement |
| `Both` | fichier + TCP |

Attention:

- en mode `Docker` seul, si serveur indisponible, il n'y a pas de fichier local de secours

## 6. Debug des pauses de job

## 6.1 Causes possibles

- pause manuelle utilisateur
- pause imposee par `BusinessSoftware`

## 6.2 Verification

1. Ouvrir `userconfig.json` et lire `BusinessSoftware`.
2. Verifier le nom de process (sans `.exe`).
3. Verifier la presence du process:

```powershell
Get-Process | Where-Object { $_.ProcessName -eq "<NomProcess>" }
```

4. Reprendre le job via `Start` apres suppression de la cause.

## 7. Debug des nouveaux parametres

## 7.1 Priority file extensions

Effet:

- les fichiers non prioritaires attendent tant qu'un job a encore des fichiers prioritaires

Verification:

- comparer l'ordre de traitement dans les logs
- observer l'avancement global via `state.json`

## 7.2 File size not to back up in parallel (KB)

Effet:

- les fichiers strictement au-dessus du seuil sont serialises (un a la fois)

Verification:

- preparer un jeu de test avec petits et gros fichiers
- comparer les temps et l'ordre d'execution

## 8. Symptomes frequents -> Actions

| Symptome | Cause probable | Action |
|---|---|---|
| Chiffrement absent | extension non configuree ou CryptoSoft introuvable | verifier extension + chemin runtime |
| Jobs bloques en pause | process metier actif ou pause manuelle | stopper process, puis `Start` |
| Pas de logs Docker | serveur non lance ou mauvais mode | lancer `LogServer`, verifier mode |
| Erreur source introuvable | dossier supprime/inaccessible | verifier chemin et droits |

## 9. Donnees a fournir dans un ticket

Toujours joindre:

- mode d'execution (GUI/CLI)
- commande ou action exacte
- extrait de `userconfig.json`
- extrait de `state.json`
- extrait de log du run concerne
- resultat du test de presence `Resources\CryptoSoft.exe`
