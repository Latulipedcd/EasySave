# Guide Utilisateur EasySave (Complet)

| Champ | Valeur |
|---|---|
| Produit | EasySave |
| Portee | GUI + mode commande |
| Version doc | 2.0 |
| Derniere mise a jour | 25/02/2026 |

## 1. Objectif

EasySave est une application de sauvegarde qui permet de:

- gerer des jobs (creation, modification, suppression, reordonnancement)
- executer un ou plusieurs jobs en parallele
- suivre l'etat des jobs en temps reel
- controler l'execution (`Start`, `Pause`, `Cancel`)
- appliquer des regles avancees (priorites d'extensions, seuil gros fichiers)
- chiffrer certains fichiers via CryptoSoft
- produire des logs locaux, Docker, ou les deux

## 2. Demarrage

## 2.1 Prerequis

- .NET SDK 10.x
- Windows recommande

Verifier:

```bash
dotnet --version
```

## 2.2 Lancer l'application

GUI:

```bash
dotnet run --project src/GUI/GUI.csproj
```

Console interactive:

```bash
dotnet run --project src/Console/Console.csproj
```

Mode commande (argument unique):

```bash
dotnet run --project src/GUI/GUI.csproj -- "1-3"
dotnet run --project src/GUI/GUI.csproj -- "1;3;5"
```

Formats acceptes:

- `1-3` pour une plage
- `1;3;5` pour une selection explicite

## 3. Interface GUI

## 3.1 Ecran principal

- Zone gauche: liste des jobs + actions globales
- Zone droite: details du job selectionne
- Barre haute: bouton `Settings` (parametres persistants)
- Widget assistant: retour contextuel pendant les actions

## 3.2 Actions principales

- `New job`
- `Edit job` (selection unique)
- `Run selection`
- `Run all`
- `Delete selection`

La liste est reordonnable en glisser-deposer.

## 3.3 Fenetre "Run all"

Quand `Run all` est lance:

- une fenetre de suivi dediee s'ouvre
- progression globale + progression par job
- controle par job: `Start`, `Pause`, `Cancel`
- fermeture avec confirmation: peut annuler tous les jobs en cours

## 4. Gestion des jobs

## 4.1 Creation

Champs obligatoires:

- `Name`
- `Source folder`
- `Destination folder`
- `Type` (`Full` ou `Differential`)

Contraintes:

- nom non vide
- nom unique

## 4.2 Modification

- selectionner un seul job
- ouvrir `Edit job`
- sauvegarder les changements

## 4.3 Suppression

- selection simple ou multiple
- confirmation de suppression
- recharge automatique de la liste

## 5. Comportements de sauvegarde

## 5.1 Types de sauvegarde

| Type | Regle |
|---|---|
| Full | Copie tous les fichiers source vers destination |
| Differential | Copie seulement les fichiers absents ou plus recents que la destination |

## 5.2 Execution parallele

- les jobs selectionnes sont executes en parallele
- chaque job peut etre controle individuellement
- le suivi s'appuie sur `state.json`

## 5.3 Statuts de job

| Statut | Signification |
|---|---|
| Inactive | Job non demarre |
| Active | Job en cours |
| Paused | Job suspendu (manuel ou logiciel metier) |
| Completed | Job termine |
| Error | Echec pendant execution |
| Cancelled | Annulation demandee par utilisateur |

## 5.4 Pause et reprise

Deux causes de pause existent:

- pause manuelle (action utilisateur)
- pause logicielle metier (processus surveille actif)

Un job reprend seulement quand les deux causes sont levees.

## 6. Parametres (complet)

Les parametres sont persistes dans:

- `%APPDATA%\EasySave\userdata\userconfig.json`

| Parametre GUI | Valeurs | Defaut | Effet |
|---|---|---|---|
| Language | `en`, `fr` | `en` | Change les textes UI |
| Log format | `Json`, `Xml` | `Json` | Format des logs locaux et/ou Docker |
| Log storage mode | `Local`, `Docker`, `Both` | `Local` | Cible de sortie des logs |
| Business software to block | Nom de processus (sans `.exe`) | vide | Met en pause les jobs quand le processus tourne |
| File extensions to encrypt | Liste CSV (`.txt, .pdf`) | vide | Fichiers traites via CryptoSoft |
| Priority file extensions | Liste CSV (`.sql, .docx`) | vide | Priorite inter-jobs: ces extensions passent avant les autres |
| File size not to back up in parallel (KB) | Entier >= 0 | `0` | Si > 0, les fichiers plus gros sont serialises (un a la fois) |

Notes:

- les extensions sont traitees de maniere insensible a la casse
- un point initial est accepte ou ajoute automatiquement (`txt` => `.txt`)
- valeur `0` pour le seuil de taille = regle desactivee

## 7. Chiffrement CryptoSoft

## 7.1 Principe

Pour chaque fichier:

1. EasySave verifie si l'extension est dans la liste a chiffrer
2. si oui, EasySave lance CryptoSoft
3. sinon, copie standard

## 7.2 Emplacements

Source dans le depot:

- `src/Application/Resources/CryptoSoft.exe`

Chemin attendu au runtime:

- `<dossier de sortie application>\Resources\CryptoSoft.exe`

## 7.3 Fallback

Si `CryptoSoft.exe` est introuvable:

- EasySave bascule en copie standard
- la sauvegarde continue

## 8. Logs, etat, fichiers persistants

| Element | Chemin Windows par defaut | Utilisation |
|---|---|---|
| Jobs | `%APPDATA%\EasySave\Jobs\jobs.json` | Liste des jobs |
| Config utilisateur | `%APPDATA%\EasySave\userdata\userconfig.json` | Parametres persistants |
| Etat live | `%APPDATA%\EasyLog\Progress\state.json` | Suivi en temps reel |
| Logs locaux | `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml` | Journal d'execution |

## 8.1 Contenu utile des logs

Champs clefs:

- `BackupName`
- `Source`, `Target`
- `WorkType` (`file_transfer`, `folder_creation`, `encryption`)
- `FileSize`
- `Duration`
- `EncryptionTimeMs`
- `ErrorMessage`
- `UserName`

## 8.2 Modes de stockage des logs

| Mode | Comportement |
|---|---|
| Local | Ecriture fichier locale uniquement |
| Docker | Envoi TCP vers serveur de logs uniquement |
| Both | Ecriture locale + envoi Docker |

## 9. Mode Docker (logs)

EasySave envoie les logs Docker vers:

- `127.0.0.1:11000`

Demarrer le serveur de logs:

```bash
dotnet run --project LogServer/LogServer.csproj
```

Si le mode est `Docker` seulement et que le serveur n'est pas lance:

- pas de logs locaux
- pertes possibles de traces d'execution

## 10. Commande globale `EasySave`

Au demarrage sous Windows, l'application tente d'installer un shim CLI.

Scripts manuels:

```bat
scripts\install-easysave-cli.cmd
scripts\uninstall-easysave-cli.cmd
```

Si la commande globale n'est pas disponible, utiliser `dotnet run --project ... -- "<selection>"`.

## 11. Depannage (runbook)

## 11.1 Chiffrement non applique

Verifier:

1. extensions de chiffrement configurees
2. presence runtime de `Resources\CryptoSoft.exe`
3. logs (`WorkType`, `EncryptionTimeMs`)

## 11.2 Jobs bloques en pause

Verifier:

1. processus metier surveille encore actif
2. pause manuelle non levee
3. action `Start` relancee apres correction

## 11.3 Aucun log Docker

Verifier:

1. mode de stockage = `Docker` ou `Both`
2. `LogServer` lance
3. port `11000` en ecoute

## 11.4 Erreurs de source introuvable

Verifier:

1. existence du dossier source
2. droits d'acces lecture
3. log d'erreur dans les journaux

## 12. Bonnes pratiques

- Utiliser des noms de jobs stables et explicites.
- Commencer avec `Local` pour valider les logs, puis activer `Both`.
- Definir des extensions prioritaires seulement si necessaire.
- Garder `MaxParallelFileSizeKb = 0` tant qu'aucune contrainte memoire n'est observee.
- Tester d'abord sur un jeu de donnees reduit avant execution massive.

## 13. Documentation associee

- Guide complet EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- Guide synthese: `docs/user-guide/USER_GUIDE_ONE_PAGE.md`
- Guide debug: `docs/debug/DEBUG_GUIDE.md`
- Guide tests unitaires: `docs/testing/UNIT_TESTS.md`
