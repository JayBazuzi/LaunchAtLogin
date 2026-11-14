---
name: dotnet-cli
description: Use whenever creating or editing a .NET (C#/F#) command-line program, console app, or CLI, including argument parsing, options, subcommands, or help text.
---

# .NET Command-Line Programs

Use the `System.CommandLine` package for argument parsing in every .NET command-line program.
Do not hand-roll parsing of `args`, and do not use third-party parsers.

## Version

A prerelease version is fine, and preferred if it is newer than the latest stable release.
Add it with:

```
dotnet add package System.CommandLine --prerelease
```

The API has changed between prereleases, so check the installed version's API (and its docs) rather than relying on memory or old samples.

## Guidelines

- Define options, arguments, and subcommands with `System.CommandLine` so that `--help` and error messages come for free.
- Return a meaningful exit code: 0 for success, non-zero for failure.
- Write results to stdout and diagnostics to stderr.
