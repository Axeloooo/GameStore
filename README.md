# GameStore

GameStore is a full-stack video game store application built with .NET for browsing and purchasing video games.

## Documentation

- [Setup](#setup)
- [Git Workflow](#git-workflow)
- [Branch Naming Convention](#branch-naming-convention)
- [Commit Message Convention](#commit-message-convention)
- [Contributors](#contributors)
- [License](#license)

## Setup

> [!IMPORTANT]
>
> If you are using Windows or Linux, you need to install the following programs using the package manager of your operating system. The following instructions use Homebrew, which is a package manager for Mac OS.

The following programs should be installed:

- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Git Workflow

- The `devel` branch is the default branch.
- The `main` branch is the production branch.

## Branch Naming Convention

- Feature branches should be named as `feature/<feature-name>`.
- Bugfix branches should be named as `fix/<bugfix-name>`.

The branche name should be in the following format:

```bash
git checkout -b feature/add-chapter-for-this-topic
```

## Commit Message Convention

The basic structure includes:

- `fix`: for bug fixes
- `feat`: for new features

The commit message should be in the following format:

```bash
git commit -m "feat: Added chapter for this topic"
```

## Contributors

- [Axel Sanchez](https://github.com/Axeloooo)

## License

[MIT](https://opensource.org/licenses/MIT)
