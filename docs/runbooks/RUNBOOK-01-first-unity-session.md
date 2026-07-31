# RUNBOOK-01: First Unity Session

- Audience: the coding agent running inside VS Code on the session machine
- Repository: ClaudeArena
- Branch: `arena/019fb2a8-claudearena`
- Required baseline ancestor: `b7918334c5dfc15e4bf94cfca150ced72c884e70`
- Actual session HEAD: record at runtime; never pre-invent it
- Editor: Unity 2022.3.59f1, with both Android Build Support and iOS Build Support modules installed
- Goal: capture the untouched compile and test baseline first. Only after that baseline is preserved, allow at most five unambiguous local compile repairs. Never repair a failing test.

## Required terminal

Every shell command in this runbook must be executed in Git Bash.

Do not use `cmd.exe` or PowerShell for the command tables. If Git Bash is not available, STOP and record `GIT BASH NOT AVAILABLE`. Do not translate the commands ad hoc.

## Standing rules

1. REPORT BEFORE REPAIR. Record the real output first. Never describe an outcome you did not observe.
2. Never paste invented log text, invented counts, or invented file contents. If something was not observed, write `NOT OBSERVED`.
3. Never accept a Unity dialog offering to upgrade, update, or auto-fix anything. Decline or close it and record that it appeared.
4. This session creates no commits. Not on the working branch, not on any other branch. Preservation happens through patches and an archive, described in Phase G.
5. Never delete `Library/`, `Temp/`, `obj/`, or `Logs/`. They are ignored; leave them alone.
6. When an EXPECTED value does not match, STOP that phase, record the mismatch, and do not improvise a fix.
7. Out of scope for this session: moving CoreFactory into a package, writing `package.json`, implementing ADR-0001, importing TMP Essential Resources, font fallbacks, ad SDK, ATT, localization, creating `AndroidManifest.xml`.
8. If `GameFactory/Tools/rollback_handler.py` is used, run `git status` immediately afterwards. Recovered files can appear already staged.

## Known conditions derived from the repository

These are derived from the code, not guessed. They are expectations, not guarantees.

- K-01 Unity will generate at least 32 new `.meta` entries (21 missing file metas plus 11 missing folder metas), plus more for anything it creates itself.
- K-02 `FactoryPreflight` will report a P0 error about empty font references and will block builds. This gate was added deliberately in WP-1.
- K-03 `UIThemeGenerator` looks for `LiberationSans SDF` at a fixed path that exists only after TMP Essential Resources are imported. That import is out of scope here, so `primaryFont` is expected to stay null.
- K-04 UPM needs Git on PATH to resolve the `unity-mcp` dependency.
- K-05 `packages-lock.json` will contain dependencies absent from `manifest.json`: imageconversion, physics, uielements, screencapture, unitywebrequest, `newtonsoft-json` 3.0.2, and a resolved `test-framework`. This is expected, not contamination.
- K-06 The existing `HapticBridge.mm.meta` uses keys that are not part of Unity's PluginImporter schema. Whether Unity rewrites it and whether the ARC flag survives is UNVERIFIED and must be measured, not assumed.
- K-07 Three EditMode tests call `DontDestroyOnLoad`, `Destroy`, and `StartCoroutine`, which behave differently outside play mode. Failure risk is high; the actual result is unknown.
- K-08 No `Assets/CoreFactory/Resources` folder exists yet. The generators target `Assets/CoreFactory/Resources` and its `Generated` subfolder inside the host project, not `Assets/Resources`. Those generated outputs are ignored per WP-1, but any newly created folder metadata that is not ignored must still be inspected.
- K-09 17 C# files have never been compiled by any compiler.
- K-10 `Assets/Plugins/Android/AndroidManifest.xml` does not exist, so the Android preflight check will fail.

## Phase A: environment baseline, before opening Unity

| Step | Command | Expected | Stop condition |
|---|---|---|---|
| A1 | `git rev-parse --abbrev-ref HEAD` | `arena/019fb2a8-claudearena` | any other branch |
| A2 | `git rev-parse HEAD` | record the actual SHA verbatim | command fails |
| A3 | `git merge-base --is-ancestor b7918334c5dfc15e4bf94cfca150ced72c884e70 HEAD` | exit code 0 | nonzero exit |
| A4 | `git status --porcelain` | empty | any output |
| A5 | `git --version` | prints a version | command not found |
| A6 | `test -d ProjectSettings` | absent | present |
| A7 | `test -f Packages/packages-lock.json` | absent | present |

Record all seven outputs verbatim.

## Phase B: first open

| Step | Action | Record | Stop condition |
|---|---|---|---|
| B1 | Open the project in Unity 2022.3.59f1 | start and end time | Unity asks to change editor version |
| B2 | Any dialog offering upgrade, update, or auto-fix | its exact title and text; decline it | none |
| B3 | Copy `Editor.log` out of the project folder before doing anything else | file saved | log not found |
| B4 | `cat ProjectSettings/ProjectVersion.txt` | both lines; `m_EditorVersion` must read `2022.3.59f1` | any other version |
| B5 | `test -f Packages/packages-lock.json` | present | absent |
| B6 | `grep -A3 coplaydev Packages/packages-lock.json` | resolved at `c14de1e6dc01ab42d2bb358730cff954bce0ce6b` | absent, or a different revision |
| B7 | `grep -A2 render-pipelines.universal Packages/packages-lock.json` | record the resolved version; compare with `14.0.11` | record only; change nothing |
| B8 | `git diff -- Packages/manifest.json` | record whether Unity rewrote it | record only |
| B9 | Console error count and the full text of every error | verbatim | see Phase B-repair |

### Phase B-repair: bounded compile repair

Enter only if B9 shows compile errors, and only after B3 and B9 are fully recorded. The untouched baseline must exist before anything is changed.

- Maximum five iterations. After the fifth, STOP and report unconditionally.
- For each iteration record: the verbatim original error, the file and line, the exact change made, and the compile result afterwards.
- After each iteration, write the change to its own patch and keep it: `git diff --binary > ../session-fix-NN.patch`
- Create no commits.
- Permitted: fixing an unambiguous local defect in the file the compiler named. A missing using directive, a wrong identifier, an obsolete API with exactly one correct replacement in this Unity version.
- Forbidden: deleting or disabling a test, stubbing a method, wrapping code in `#if false`, suppressing a warning, changing behaviour so an error disappears, or touching anything the error did not name.
- If a fix would require choosing between designs, STOP and report.
- Never repair a failing test in this session.

## Phase C: baseline capture

| Step | Command | Expected | Stop condition |
|---|---|---|---|
| C1 | `git status --porcelain -uall` | a long list of new files | none |
| C2 | `git status --porcelain -uall \| grep -E '^\?\? (Library\|Temp\|obj\|Logs)/'` | no output | any output, meaning `.gitignore` failed |
| C3 | `git status --porcelain -uall \| grep -c '\.meta$'` | at least 32 | fewer than 32 |
| C4 | Console with Collapse on and all three filters on | the error and warning counts | none |

## Phase D: preflight behaviour

| Step | Action | Expected | Stop condition |
|---|---|---|---|
| D1 | Search the console for the preflight font error | present, per K-02 | absent, meaning either preflight did not run or the theme loaded unexpectedly; both are findings |
| D2 | `ls -R Assets/CoreFactory/Resources` | records what was generated | none |
| D3 | Select the generated `UITheme.asset` in the Inspector | record whether it shows a valid `UIThemeAsset` or a missing script | none |
| D4 | Switch the active build target to Android, then run the existing menu item `CoreFactory > Run Project Preflight Checks` | preflight reports failure | it reports success |
| D5 | Optionally attempt a real Android build | the build is blocked | the build succeeds, meaning the gate is not working |

Repair nothing in Phase D. Record only.

## Phase E: native plugin metadata

| Step | Action | Record | Stop condition |
|---|---|---|---|
| E1 | `git diff -- Assets/Plugins/iOS/HapticBridge.mm.meta` | the content Unity wrote | none |
| E2 | Switch the active build target to iOS, then run the existing menu item `CoreFactory > Run Project Preflight Checks` | the console line the preflight prints about the ARC compile flag | the preflight reports the flag is missing |
| E3 | If the iOS build target cannot be selected because the module is missing | record `NOT OBSERVED` and STOP this phase | none |

No new diagnostic script is needed. `FactoryPreflight.RunChecks` already reads the iOS compile flag through `PluginImporter.GetPlatformData` and prints the result, and it is already reachable from that menu item. Do not write a temporary script for this.

## Phase F: tests

| Step | Action | Record | Stop condition |
|---|---|---|---|
| F1 | Open Test Runner, EditMode tab | the number of DISCOVERED tests | the count is not 12 |
| F2 | Distinguish "zero discovered" from "twelve discovered" | which one occurred | none |
| F3 | Run all EditMode tests | pass or fail per test, with the full failure text | any failure |

Do not repair a failing test. A failing test is information; turning it green without understanding it destroys that information.

## Phase G: preservation and reporting

This session commits nothing. Preservation is by archive and patch.

### G1 Build the preservation allowlist

Run: `git status --porcelain -uall`

Every listed path must fall into exactly one of these categories:

- a file that was already tracked before the session
- a `.meta` file
- a path under `ProjectSettings/`
- `Packages/packages-lock.json`
- a path under `Assets/CoreFactory/Resources/`

If any path falls outside all five categories, STOP, record it, and do not archive until it has been reviewed. "Archive everything" is not an acceptance criterion.

### G2 Archive

After G1 passes, archive only these paths:

- `ProjectSettings/`
- `Packages/`
- `Assets/`

Never archive `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `.vs/`, `.idea/`, `*.csproj`, or `*.sln`.

### G3 Carry off the machine

- the archive from G2
- `Editor.log`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/packages-lock.json`
- every `session-fix-NN.patch`
- the output of `git status --porcelain -uall`
- the output of `git diff --binary`
- the session report

### G4 Report

Produce one report containing every recorded value from Phases A through F. Mark anything that was not observed as `NOT OBSERVED`.

### G5 Wait

Nothing is merged, committed, or pushed until the report has been reviewed.
