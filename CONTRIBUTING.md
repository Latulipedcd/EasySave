# Contributing Guidelines

Thank you for your interest in contributing to this project 🎉  
This document explains how to contribute effectively and consistently.

---

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a branch from `main`
4. Make your changes
5. Open a Pull Request

---

## Branching Strategy

- `main` → stable branch, always deployable
- Feature branches must follow this pattern:
    feature/<short-description>
    bugfix/<short-description>
    refactor/<short-description>
Examples:
- `feature/user-authentication`
- `bugfix/null-reference-login`

---

## Commit Message Convention

This project follows **Conventional Commits**.

### Format

<type>(optional scope): <description>
### Allowed types
- **feat** : add a new feature
- **fix** : fix a bug
- **docs** : documentation only (README, comments, guides, etc.)
- **style** : formatting, spacing, linting (no code logic change)
- **refactor** : code refactoring without adding features or fixing bugs
- **perf** : performance improvements
- **test** : add or update tests
- **build** : changes affecting the build system or dependencies (npm, NuGet, Webpack, etc.)
- **ci** : CI/CD configuration or scripts (GitHub Actions, GitLab CI, etc.)
- **chore** : general maintenance (dependency updates, cleanup, scripts)
- **revert** : revert a previous commit

Example:
  feat(auth): add JWT authentication

---

## Code Style

- Follow the coding conventions of the language/framework used: https://learn.microsoft.com/fr-fr/dotnet/csharp/fundamentals/coding-style/identifier-names
- Keep code readable and well-structured
- Avoid unnecessary complexity
- Comment complex or non-obvious logic

---

## Tests

- All new features must include tests
- Bug fixes should include regression tests when possible
- Ensure all tests pass before opening a PR

---

## Pull Request Guidelines

- One PR = one purpose
- Keep PRs small and focused
- Use the provided Pull Request template
- Link related issues in the PR description
- The PR must be reviewed and approved before merging

---

## Issue Guidelines

- Use the appropriate Issue template
- Provide clear reproduction steps for bugs
- Add screenshots or logs when relevant

---

## Documentation

- Update documentation when behavior changes
- Keep documentation concise and accurate

---

## Review Process

- At least one approval is required before merging
- Address review comments respectfully and promptly
- Do not merge your own Pull Request unless explicitly allowed

---

## Code of Conduct

Be respectful and constructive in discussions.  
Harassment, disrespectful behavior, or toxic communication will not be tolerated.

---

## Questions

If you have questions:
- Open a **Question issue**
- Or start a discussion if enabled

Thank you for contributing 🚀

