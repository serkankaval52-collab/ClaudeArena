# 🏭 GAMEFACTORY ORCHESTRATION LAYER — SETUP BLUEPRINT v3.3
## Decoupled Pipeline, Automation Scripts & Command Skeletons

This document outlines the official automation, CLI commands, and database structures for the GameFactory Orchestration Layer (v3.3.0).

---

## SECTION 1: SYSTEM AUTOMATION SCRIPTS

### 1.1 Guarded Git Worktree Restore Tool
#### File: `GameFactory/Tools/rollback_handler.py`

The source file is canonical and is intentionally not duplicated in this blueprint. Its safety contract is:

* Validate the Git repository and resolve the current `HEAD` before any mutation.
* Run and verify `git stash push -u` before reset. A stash failure must stop the operation.
* When a stash is created, create `recovery/rollback-<timestamp>` at that exact stash commit and verify the ref before reset.
* Restore the working tree only to the explicitly reported current `HEAD`; never claim that `HEAD` is a separately verified stable revision.
* Preserve the stash entry and recovery branch so tracked and untracked files remain recoverable.
* Support `--dry-run` without changing refs, the index, or the working tree.

Visual, audio, and gameplay quality must be evaluated with explicit Unity EditMode/PlayMode checks and human review. Static keyword scans must not act as a production quality gate.

### 1.2 Multi-Language Translator Tool
#### File: `GameFactory/Tools/L10n_translator.py`

The source file is canonical and is intentionally not duplicated in this blueprint. Its contract is:

* `GameFactory/Tools/L10n_translator.py` is the single source of truth for every localized string.
* The ten `L10n_Table_*.json` checkpoint files are generated output, not hand-edited content.
* After any change to the dictionary, regenerate into a temporary directory and confirm a 10/10 byte match against the committed checkpoints.
* The English `consent_description` must stay byte-identical to the hardcoded fallback string in `FallbackConsentDialog.cs`.
* Consent wording requires linguistic and legal review before release. Nothing here certifies compliance.
* These JSON files are not wired to the Unity runtime and are not currently packaged into a player build.

---

## SECTION 2: DATABASES AND SCHEMAS

### File: `GameFactory/Factory_Configs/factory_config.yaml`
```yaml
factory_version: "3.3.0"
active_game_name: ""
target_platform: "Android"
mcp_port: 8080
auto_rollback: true
current_phase: 0
```

### File: `GameFactory/Factory_Configs/ideation_library.yaml`
```yaml
mechanics:
  gravity_runner:
    variants: [lane_changer, gravity_flip]
    complexity: 2
    meta_progression:
      currency: "Gold Coins"
      levels: 30
      unlocks: ["Skin Palette 1", "Skin Palette 2", "Speed Boost Powerup"]
    description: "Run forward on procedural tiles. Press to flip gravity. Collect gold coins to unlock shop colors."
  timing_stacker:
    variants: [stack_perfect, timing_balance]
    complexity: 2
    meta_progression:
      currency: "Tower Gems"
      levels: 50
      unlocks: ["Glow Material", "Retro Tower Skin"]
    description: "Align sliding platforms sequentially. Earn gems for perfect alignments. Unlock neon block skins."

themes:
  neon_cyber:
    palette: ["#0B0C10", "#1F2833", "#C5C6C7", "#66FCF1", "#45A29E"]
    particles: "Neon_Spark_Burst"
    asset_keywords: ["arcade", "cyberpunk", "neon", "laser", "grid"]
  pastel_zen:
    palette: ["#F7E7CE", "#E2B0AA", "#A992A0", "#7C7F93", "#4E586E"]
    particles: "Petal_Drift_Wind"
    asset_keywords: ["pastel", "paper", "zen", "relaxing", "origami"]
```

### File: `GameFactory/Factory_Configs/design_presets.yaml`
```yaml
canvas_scaler:
  reference_resolution: { width: 1080, height: 1920 }
  scaling_mode: "ScaleWithScreenSize"
  screen_match: 0.5

juice_standards:
  camera_shake:
    intensity: 0.25
    duration: 0.2
  slow_mo:
    near_miss_factor: 0.2
    duration_unscaled: 0.5
  sfx_pitch_range: { min: 0.8, max: 1.3 }
```

### File: `GameFactory/Factory_Configs/tuning_dictionary.yaml`
```yaml
parameters:
  coyoteTimeDuration:
    script: "CoyoteController.cs"
    variable: "coyoteTimeDuration"
    max_limit: 0.25
  nearMissDistanceThreshold:
    script: "NearMissDetector.cs"
    variable: "nearMissDistanceThreshold"
    max_limit: 0.40
```

---

## SECTION 3: SYSTEM COMMAND DEFINITIONS

### File: `.claude/commands/new-game.md`
```markdown
# CLI COMMAND: /new-game

This command launches the complete automatic compilation of a new mobile game using the Game Factory Pipeline.

## Steps:
1.  **Initialize Pipeline State:** Check `GameFactory/Factory_Configs/factory_config.yaml` and set `current_phase: 1`. Log phase starting in `GameFactory/Factory_Logs/progress.md`.
2.  **Run Phase 1 (Ideation):** Run `/ideate` to output 2 unique game concepts combining mechanics and themes from `ideation_library.yaml`. Present them to the User. Stop and wait for User choice.
3.  **Run Phase 2 (Design):** Once concept is chosen, generate the GDD, choose the Monetization model, select the theme palette, and write the active configuration into `state.json`. Seek GDD approval.
4.  **Run Phase 3 (Compliance Gate):** Generate App metadata, dynamic Privacy Policy, ATT scene template settings, and ensure target level metrics meet strict anti-spam requirements.
5.  **Run Phase 4 (Asset Pipeline):** Source appropriate assets, configure color profiles, populate the asset library paths, and output credit logs.
6.  **Run Phase 5 (Canvas & Consent Assembly):** Instantiates the EventSystem, canvas UI components, and safe area overlay.
7.  **Run Phase 6 (Project Setup):** Programmatically set up the empty scenes (`Splash`, `Menu`, `Game`, `Results`) and instantiate the `GameManager`, `StateManager`, `EventBus`, and basic UI Layouts using Unity MCP.
8.  **Run Phase 7 (Core Gameplay & UI Code):** Generate C# gameplay script templates corresponding to chosen mechanics. Build UI overlays with TMPro, scoring fields, and double-reward buttons.
9.  **Run Phase 8 (Audio & Monetization):** Attach basic synth tracks or CC0 loops to GameManager audio source, hook up mock interstitial cooldowns, and implement IAP "Restore Purchases" logic.
10. **Run Phase 9 (Polish & Juiciness):** Inject camera shake effects, visual particle sparks on victory/loss, and haptic triggers.
11. **Run Phase 10 (Testing & Quality Assurance):** Automatically invoke Unity Test Runner via `run_tests` MCP command. Output result logs. Show the game to the User in Editor for manual play verification.
12. **Run Phase 11 (Build Pipeline):** Invoke Unity compiler tools to output clean `.aab` for Android or export an Xcode project directory for iOS review.
```

### File: `.claude/commands/factory-check.md`
```markdown
# CLI COMMAND: /factory-check

Verify the state and connection health of the factory.

## Verification Checklist:
*   [ ] Connect to Unity Editor on port 8080. Verify via ping.
*   [ ] Verify Python execution environment (`python --version`).
*   [ ] Ensure `GameFactory/Factory_Configs/` directory contains all default library configurations.
*   [ ] Check if the Core framework C# scripts exist and compile cleanly.
*   [ ] Check current pipeline state on `GameFactory/Factory_Logs/progress.md`.
```
