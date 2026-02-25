# EasySave - Guide des Tests Unitaires

| Champ | Valeur |
|---|---|
| Projet de tests | `EasySave.Tests` |
| Cible | `net10.0` |
| Framework | xUnit + Moq + coverlet.collector |
| Derniere mise a jour | 25/02/2026 (commandes verifiees) |

## 1. Objectif

Ce guide explique:

- les tests existants
- comment les lancer
- comment mesurer la couverture
- comment ajouter des tests de qualite homogène

Important:

- si vous utilisez uniquement la version utilisateur (`EasySave.exe`), les tests unitaires ne sont pas executables
- les commandes ci-dessous sont destinees au package source/equipe QA

## 2. Perimetre actuel

Le projet `EasySave.Tests` couvre principalement la logique du coeur applicatif:

| Fichier de test | Cible fonctionnelle |
|---|---|
| `BackupJobTests.cs` | Validation du modele `BackupJob` |
| `BackupFileFilterTests.cs` | Filtrage full/differential + extensions prioritaires |
| `BackupStateTests.cs` | Calcul progression + transitions de statuts |
| `BackupStateServiceTests.cs` | Initialisation, ecriture de progression, finalisation |
| `BackupValidationServiceTests.cs` | Verification source + blocage logiciel metier |

Ce qui n'est pas couvert actuellement:

- rendu GUI Avalonia
- orchestration complete multi-jobs (integration)
- pipeline end-to-end avec LogServer/Docker

## 3. Execution des tests

Depuis la racine du depot:

```bash
dotnet test EasySave.slnx
```

Execution du seul projet de tests:

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj
```

Verification effectuee le 25/02/2026:

- `dotnet test EasySave.Tests/EasySave.Tests.csproj` -> OK
- `dotnet test EasySave.slnx` -> OK
- 38 tests passes, 0 echec

## 4. Execution avec couverture

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj --collect:"XPlat Code Coverage"
```

Sortie coverage:

- `EasySave.Tests/TestResults/<run-id>/coverage.cobertura.xml`

## 5. Filtres utiles

Par classe:

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj --filter "FullyQualifiedName~BackupFileFilterTests"
```

Par nom de methode:

```bash
dotnet test EasySave.Tests/EasySave.Tests.csproj --filter "Name~IsSourceDirectoryMissing"
```

## 6. Standard de qualite pour nouveaux tests

## 6.1 Convention de nommage

Format recommande:

- `Methode_Contexte_ResultatAttendu`

Exemple:

- `ExecuteBackupAsync_WhenPaused_WritesPausedState`

## 6.2 Structure recommande

```csharp
[Fact]
public void Methode_Contexte_ResultatAttendu()
{
    // Arrange
    // Act
    // Assert
}
```

## 6.3 Regles pratiques

- un comportement principal par test
- assertions precises (eviter les assertions vagues)
- tests deterministes (pas de dependance a l'heure systeme sans controle)
- isolation des IO via dossiers temporaires
- dependances exterieures mockees via interfaces

## 7. Patterns utilises dans le projet

- `Moq` pour `IFileService`, `IProgressWriter`, `IBusinessSoftwareMonitor`, `IBackupLoggerService`
- dossiers/fichiers temporaires pour scenarios filesystem
- verification des appels de log/progression (`Verify(...)`)

## 8. Roadmap de tests recommandees

Priorites hautes:

1. `JobExecutionService`: parallelisme, pause/reprise, cancel.
2. `EncryptionService`: fallback CryptoSoft absent, codes d'erreur.
3. `SharedExecutionContext`: coordination priorites + gros fichiers.
4. `UserConfigManager`: robustesse sur JSON invalide.
5. `JobStateFileReader`: comportement sur fichier partiel/corrompu.

Priorites moyennes:

1. `DockerLoggerService` avec faux serveur TCP.
2. tests integration "small scenario" (jobs + state + logs).

## 9. Controle avant merge

Checklist minimale:

1. `dotnet test` passe sans echec.
2. Les nouveaux tests couvrent le comportement ajoute.
3. Les assertions valident les effets observables (etat, logs, retours).
4. Aucun test fragile lie a l'environnement local.

## 10. Conseils CI

- executer `dotnet test EasySave.slnx` sur chaque PR
- publier le rapport de couverture en artifact
- bloquer merge si tests rouges

## 11. References

- Full guide EN: `docs/user-guide/en/EasySave_User_Guide_Full.md`
- Guide complet FR: `docs/user-guide/fr/Guide_Utilisateur_EasySave_Complet.md`
- Guide debug: `docs/debug/DEBUG_GUIDE.md`
