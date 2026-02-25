# Guide Utilisateur EasySave (Complet)

| Champ | Valeur |
|---|---|
| Produit | EasySave |
| Public | Utilisateurs finaux (version executable) |
| Portee | EasySave.exe + dossier Resources |
| Version doc | 2.1 |
| Derniere mise a jour | 25/02/2026 |

## 1. Ce que contient votre dossier EasySave

Votre package utilisateur doit contenir au minimum:

- `EasySave.exe`
- `Resources\Languages\`
- `Resources\CryptoSoft.exe`

Ce guide est ecrit pour ce mode d'utilisation (pas pour un depot source).

## 2. Demarrage

## 2.1 Lancement standard (interface graphique)

Double-cliquer sur:

- `EasySave.exe`

L'application ouvre la fenetre principale avec:

- la liste des jobs
- les boutons d'action (`New job`, `Edit job`, `Run selection`, `Run all`, `Delete selection`)
- le panneau de details
- le menu `Settings`

## 2.2 Lancement en mode commande (facultatif)

Depuis un terminal ouvert dans le dossier de l'application:

```bat
EasySave.exe "1-3"
EasySave.exe "1;3;5"
```

Formats de selection:

- `1-3`: plage d'IDs
- `1;3;5`: IDs explicites

## 3. Workflow utilisateur

1. Creer un job (`New job`)
2. Renseigner nom, source, destination, type
3. Sauvegarder
4. Selectionner un ou plusieurs jobs
5. Lancer (`Run selection` ou `Run all`)
6. Suivre et piloter (`Start`, `Pause`, `Cancel`)

Pour un guide visuel illustre:

- HTML FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.html`
- PDF FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.pdf`

## 4. Gestion des jobs

## 4.1 Creation

Champs:

- `Name` (obligatoire, unique)
- `Source folder` (obligatoire)
- `Destination folder` (obligatoire)
- `Type` (`Full` ou `Differential`)

## 4.2 Modification

- selectionner exactement un job
- ouvrir `Edit job`
- modifier puis sauvegarder

## 4.3 Suppression

- selection simple ou multiple
- confirmer la suppression

## 5. Comportement de sauvegarde

| Type | Regle |
|---|---|
| Full | Copie tous les fichiers de la source |
| Differential | Copie les fichiers absents ou plus recents que la destination |

Execution:

- jobs en parallele
- controle runtime par job
- suivi live via `state.json`

Statuts possibles:

- `Inactive`, `Active`, `Paused`, `Completed`, `Error`, `Cancelled`

## 6. Parametres (Settings)

Les parametres sont sauvegardes dans:

- `%APPDATA%\EasySave\userdata\userconfig.json`

| Parametre | Valeurs | Effet |
|---|---|---|
| Language | `en`, `fr` | Langue interface |
| Log format | `Json`, `Xml` | Format des logs |
| Log storage mode | `Local`, `Docker`, `Both` | Cible des logs |
| Business software to block | nom de process (sans `.exe`) | Pause automatique si process detecte |
| File extensions to encrypt | liste libre separee par virgules | Chiffrement via CryptoSoft |
| Priority file extensions | liste libre separee par virgules | Traitement prioritaire inter-jobs |
| File size not to back up in parallel (KB) | entier >= 0 | Gros fichiers serialises au-dessus du seuil |

Important sur les extensions:

- ce n'est pas un CSV strict (pas de guillemets/format special)
- c'est une saisie texte simple, ex: `.txt, txt, .log`
- casse ignoree (`.TXT` = `.txt`)

## 7. CryptoSoft (version utilisateur)

Chemin a verifier:

- `Resources\CryptoSoft.exe`

Comportement:

1. si extension correspondante: tentative de chiffrement
2. sinon: copie normale
3. si CryptoSoft absent: fallback en copie normale

## 8. Fichiers utiles

| Element | Chemin Windows par defaut | Utilisation |
|---|---|---|
| Config utilisateur | `%APPDATA%\EasySave\userdata\userconfig.json` | Parametres persistants |
| Jobs | `%APPDATA%\EasySave\Jobs\jobs.json` | Definition des jobs |
| Etat live | `%APPDATA%\EasyLog\Progress\state.json` | Suivi execution |
| Logs locaux | `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml` | Historique execution |

## 9. Depannage rapide

## 9.1 Chiffrement non applique

Verifier:

1. presence de `Resources\CryptoSoft.exe`
2. extensions configurees dans Settings
3. logs (`WorkType`, `EncryptionTimeMs`)

## 9.2 Jobs bloques en pause

Verifier:

1. process metier encore actif
2. pause manuelle non levee
3. reprise via `Start`

## 9.3 Pas de logs Docker

Verifier:

1. mode `Docker` ou `Both`
2. serveur de logs actif sur `127.0.0.1:11000`

## 10. Bonnes pratiques

- noms de jobs courts et explicites
- tester un petit jeu de donnees avant un gros run
- commencer en logs `Local`, puis passer en `Both` si besoin Docker
- n'activer les extensions prioritaires qu'en cas de besoin reel

## 11. Documentation associee

- Full guide EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- Guide synthese FR: `docs/user-guide/fr/USER_GUIDE_ONE_PAGE.md`
- Guide synthese EN: `docs/user-guide/en/USER_GUIDE_ONE_PAGE_EN.md`
- Guide debug: `docs/debug/DEBUG_GUIDE.md`
- Guide tests: `docs/testing/UNIT_TESTS.md`
