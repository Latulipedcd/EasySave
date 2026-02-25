# EasySave - Guide Synthese (1 page, FR)

## 1. Lancement

Dans le dossier de l'application:

- double-cliquer `EasySave.exe`

Mode commande (facultatif):

```bat
EasySave.exe "1-3"
EasySave.exe "1;3;5"
```

## 2. Workflow express

1. `New job`
2. Renseigner `Name`, `Source`, `Destination`, `Type`
3. `Save`
4. Selectionner des jobs
5. `Run selection` ou `Run all`
6. Piloter avec `Start`, `Pause`, `Cancel`

Pour un guide visuel:

- `docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.html`
- `docs/user-guide/fr/Guide_Utilisateur_EasySave_GUI.pdf`

## 3. Types de sauvegarde

- `Full`: copie tous les fichiers
- `Differential`: copie les fichiers absents ou plus recents

## 4. Parametres critiques

| Parametre | Valeurs | Impact |
|---|---|---|
| Language | `en`, `fr` | Langue UI |
| Log format | `Json`, `Xml` | Format de log |
| Log storage mode | `Local`, `Docker`, `Both` | Destination des logs |
| Business software to block | Nom de process | Pause auto si process actif |
| File extensions to encrypt | liste libre separee par virgules | Chiffrement via CryptoSoft |
| Priority file extensions | liste libre separee par virgules | Priorite inter-jobs |
| File size not to back up in parallel (KB) | entier >= 0 | Seuil de serialisation gros fichiers |

Note extensions:

- saisie texte simple, pas CSV strict
- exemple: `.txt, txt, .log`

## 5. Chemins utiles (Windows)

- Config: `%APPDATA%\EasySave\userdata\userconfig.json`
- Jobs: `%APPDATA%\EasySave\Jobs\jobs.json`
- Etat live: `%APPDATA%\EasyLog\Progress\state.json`
- Logs locaux: `%APPDATA%\EasyLog\Logs\log-YYYY-MM-DD.json|xml`
- CryptoSoft (version utilisateur): `Resources\CryptoSoft.exe`

## 6. Depannage express

- Chiffrement absent: verifier `Resources\CryptoSoft.exe` + extensions.
- Jobs bloques en pause: verifier process metier + lever pause manuelle.
- Pas de logs Docker: verifier mode `Docker/Both` + serveur de logs actif.

## 7. Autres documents

- Guide complet FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- Full guide EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- One-page EN: `docs/user-guide/en/USER_GUIDE_ONE_PAGE_EN.md`
- Guide debug: `docs/debug/DEBUG_GUIDE.md`
- Guide tests: `docs/testing/UNIT_TESTS.md`
