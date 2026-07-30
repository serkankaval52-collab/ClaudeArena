# ADR-0001: Multi-Game Architecture — CoreFactory as a Pinned UPM Package

- Status: Accepted (design only; implementation deferred)
- Date: 2026-07-30
- Deciders: Serkan (final authority), Claude (audit), Arena (implementation)
- Supersedes: none
- Implementation: NOT STARTED. No file moves until the first verified Unity session.

## Context

ClaudeArena currently holds the CoreFactory sources under `Assets/CoreFactory` plus a native iOS plugin under `Assets/Plugins/iOS`. There is no isolation mechanism between games. The factory is intended to produce many games without cross-contamination of settings, content, or core code.

## Decision

1. Every game is its own Git repository and its own Unity project.
2. CoreFactory becomes a UPM package. ClaudeArena remains its home.
3. Games depend on CoreFactory through a Git URL pinned to an immutable commit SHA. Tag names and branch names are not used as pins.
4. Producing several games from one Unity project is technically possible via scripting defines, build profiles and custom build scripts. It is rejected here because it creates high coupling, setting leakage, and the risk of shipping the wrong product.

## Rejected alternatives

- One project, games as folders, settings swapped per build.
  Rejected: settings swapping is manual and is the most likely source of cross-contamination.
- Monorepo with CoreFactory as a local embedded package.
  Rejected: an embedded package is writable, so any game can silently diverge the core.

## Isolation is process-enforced, not mechanically guaranteed

A pinned commit is strong but not unbreakable. A game can fork, embed, or change the pin. Isolation therefore requires:

- commit pins only, never tags or branches;
- `packages-lock.json` reviewed on every change;
- no edits to package sources from inside a game project;
- a controlled upgrade procedure when the pin moves.

## Package scope

The package must contain:

- Runtime C# sources and their asmdef
- Editor C# sources and their asmdef
- Test asmdef and tests
- `HapticBridge.mm` (currently outside `Assets/CoreFactory`)
- `PrivacyInfo.xcprivacy` (currently outside `Assets/CoreFactory`)
- Native plugin import settings
- `package.json`, `README`, `CHANGELOG`, `LICENSE`

If the native plugin and privacy manifest are forgotten, games ship without haptics and without a privacy manifest.

## Repository layout consequence

A UPM Git dependency targets a subfolder of the repository. The CoreFactory content and its `package.json` must therefore live in a dedicated subfolder of ClaudeArena. The current `Assets/CoreFactory` layout cannot be consumed as a package without this move.

## Host output contract

`Application.dataPath` resolves to the host project's `Assets` folder regardless of whether the calling script runs from the package cache. Generators therefore write into the host, not into the read-only package cache.

Nevertheless, writing generated output under a path named after the package mixes package-owned and game-owned content. Output must move to an explicitly game-owned location. All host output paths must be declared in a single contract type so that a rename touches exactly one file.

## Preflight is a package-wide build gate

Installing the package activates, in every host game and on every domain reload:

- `[InitializeOnLoad]`
- `EditorApplication.delayCall`
- asset generation
- preflight checks
- `IPreprocessBuildWithReport`

This is an explicit contract, not an implementation detail.

Bypass policy:

- The production build gate is fail-closed by default.
- Any bypass must be explicitly named, must emit a high-visibility console warning, must be forbidden in CI and release builds, and must itself block production builds while active.
- Editor-idle asset generation and pre-build read-only validation are separate responsibilities and must be separated.

## Test discovery

Package tests are not discovered unless the package name is added to the host manifest's `testables` array. Required gates:

- test assembly placed per Unity's package test discovery rules;
- package name present in host `testables`;
- the number of discovered tests is verified, not assumed;
- "tests ran and passed" and "test assembly was never discovered" are reported as different outcomes;
- the CoreFactory commit under test is confirmed in `packages-lock.json`.

## Operational dependencies

- Every game project needs Git installed and network access to ClaudeArena at package resolution time.
- If ClaudeArena is ever made private, every game breaks until credentials are configured. This must be decided before release.
- The `version` field in `package.json` does not drive resolution for a Git pin; the pinned commit does. A version field that disagrees with the pinned commit is misleading and must be kept in sync by procedure.

## Open questions — DO NOT INVENT VALUES

The following are unresolved and must not be guessed. They are decided only with evidence from a real Unity session.

- Canonical package name
- Canonical package folder layout inside ClaudeArena
- Canonical host output paths for generated theme and sprite assets
- Whether `PrivacyInfo.xcprivacy` shipped from a package reaches the exported Xcode target and the final archive
- How Unity and Xcode handle multiple privacy manifests
- Whether the existing test assembly layout satisfies package test discovery unchanged

## Verification gates (all currently UNVERIFIED)

1. Package resolves in a host project from a pinned commit.
2. All CoreFactory assemblies compile inside the host.
3. Discovered EditMode test count equals the expected count.
4. Generated assets appear at the contracted host output paths.
5. Preflight blocks a build when its conditions are unmet.
6. iOS export contains the native plugin with correct compile flags.
7. iOS export and archive contain the privacy manifest.
8. A second game repository pinned to the same commit produces an identical CoreFactory state.

## Implementation sequencing

No file is moved before the first Unity session succeeds and the current sources are proven to compile. Moving files while `.meta` files do not yet exist would create GUID churn on top of an unverified codebase.
