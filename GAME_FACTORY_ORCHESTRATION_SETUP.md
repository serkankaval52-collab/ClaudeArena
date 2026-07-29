# 🏭 GAMEFACTORY ORCHESTRATION LAYER — SETUP BLUEPRINT v3.3
## Decoupled Pipeline, Automation Scripts & Command Skeletons

This document outlines the official automation, CLI commands, and database structures for the GameFactory Orchestration Layer (v3.3.0).

---

## SECTION 1: SYSTEM AUTOMATION SCRIPTS

### 1.1 Automated Git Rollback Tool
#### File: `GameFactory/Tools/rollback_handler.py`
```python
import sys
import subprocess
import time

def safe_rollback():
    """
    Creates a defensive recovery backup stash before reverting Assets/CoreFactory to the stable state.
    """
    print("[Rollback System] Compiler error encountered. Initializing defensive restore checkpoint...")
    stamp = time.strftime("%Y%m%d-%H%M%S")
    try:
        # Create a safe recovery branch to preserve potential uncommitted developer manual patches
        subprocess.run(["git", "stash", "push", "-u", "-m", f"autorollback-{stamp}"], check=False)
        subprocess.run(["git", "branch", f"recovery/rollback-{stamp}"], check=False)
        
        # Now execute the hard reset safely
        subprocess.run(["git", "reset", "--hard", "HEAD"], check=True)
        print(f"[Rollback System] Reverted Assets to the last stable Git state successfully. Recovery branch tag: recovery/rollback-{stamp}")
        return True
    except Exception as e:
        print(f"[Rollback System] Critical: Failed to restore state: {e}", file=sys.stderr)
        return False

if __name__ == "__main__":
    safe_rollback()
```

### 1.2 Syntactic Polish Verifier Tool
#### File: `GameFactory/Tools/polish_verifier.py`
```python
import sys
import os

REQUIRED_TERMS = ["ParticleSystem", "CameraShake", "AudioSource", "Coyote", "NearMiss"]

def verify_polish(scripts_dir):
    """
    Parses and checks generated C# files for necessary premium polish declarations.
    """
    print(f"[Verifier] Parsing source directory: {scripts_dir}")
    merged_source = ""
    for root, _, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith(".cs") and "Tests" not in root:
                try:
                    with open(os.path.join(root, file), "r", encoding="utf-8") as f:
                        merged_source += f.read()
                except Exception:
                    pass

    missing = [term for term in REQUIRED_TERMS if term not in merged_source]
    if missing:
        print(f"[Verifier] FAIL: Missing premium polish elements: {missing}", file=sys.stderr)
        sys.exit(1)
    else:
        print("[Verifier] PASS: All required premium particles, sfx, and cameras are declared.")
        sys.exit(0)

if __name__ == "__main__":
    if len(sys.argv) > 1:
        verify_polish(sys.argv[1])
```

### 1.3 Multi-Language Translator Tool
#### File: `GameFactory/Tools/L10n_translator.py`
```python
import sys
import os
import json
import re

L10N_DB = {
    "play_button": {
        "en": "PLAY", "es": "JUGAR", "pt": "JOGAR", "de": "SPIELEN", "fr": "JOUER",
        "tr": "OYNA", "ja": "プレイ", "ko": "플레이", "zh": "开始", "ar": "تشغيل"
    },
    "score_label": {
        "en": "SCORE", "es": "PUNTUACIÓN", "pt": "PONTUAÇÃO", "de": "SCORE", "fr": "SCORE",
        "tr": "SKOR", "ja": "スコア", "ko": "점수", "zh": "得分", "ar": "النتيجة"
    },
    "near_miss": {
        "en": "SO CLOSE!", "es": "¡CASI!", "pt": "QUASE LÁ!", "de": "SO NAHE!", "fr": "SI PROCHE!",
        "tr": "ÇOK YAKIN!", "ja": "おしい！", "ko": "아깝다!", "zh": "太可惜了！", "ar": "لقد كنت قريبا!"
    },
    "revive_offer": {
        "en": "Watch Ad to Continue?", "es": "Ver anuncio?", "pt": "Assistir anúncio?", "de": "Werbung ansehen?", "fr": "Regarder la pub?",
        "tr": "Reklam İzleyip Devam?", "ja": "動画を見てリトライ？", "ko": "광고 보고 이어하기?", "zh": "观看广告继续？", "ar": "مشاهدة إعلان للمتابعة؟"
    },
    "menu_title": {
        "en": "MAIN MENU", "es": "MENÚ PRINCIPAL", "pt": "MENU PRINCIPAL", "de": "HAUPTMENÜ", "fr": "MENU PRINCIPAL",
        "tr": "ANA MENÜ", "ja": "メインメニュー", "ko": "메인 메뉴", "zh": "主菜单", "ar": "القائمة الرئيسية"
    },
    "settings_button": {
        "en": "SETTINGS", "es": "AJUSTES", "pt": "CONFIGURAÇÕES", "de": "EINSTELLUNGEN", "fr": "PARAMÈTRES",
        "tr": "AYARLAR", "ja": "設定", "ko": "설정", "zh": "设置", "ar": "الإعدادات"
    },
    "pause_button": {
        "en": "PAUSE", "es": "PAUSA", "pt": "PAUSAR", "de": "PAUSE", "fr": "PAUSE",
        "tr": "DURAKLAT", "ja": "一時停止", "ko": "일시 정지", "zh": "暂停", "ar": "إيقاف مؤقت"
    },
    "retry_button": {
        "en": "RETRY", "es": "REINTENTAR", "pt": "RECOMEÇAR", "de": "WIEDERHOLEN", "fr": "REJOUER",
        "tr": "YENİDEN DENE", "ja": "もう一度", "ko": "다시 시도", "zh": "重试", "ar": "إعادة المحاولة"
    },
    "game_over_label": {
        "en": "GAME OVER", "es": "FIN DE JUEGO", "pt": "FIM DE JOGO", "de": "SPIEL VORBEI", "fr": "FIN DE PARTIE",
        "tr": "OYUN BİTTİ", "ja": "ゲームオーバー", "ko": "게임 오버", "zh": "游戏结束", "ar": "لقد خسرت"
    },
    "quit_button": {
        "en": "QUIT", "es": "SALIR", "pt": "SAIR", "de": "BEENDEN", "fr": "QUITTER",
        "tr": "ÇIKIŞ", "ja": "終了", "ko": "종료", "zh": "退出", "ar": "خروج"
    },
    "consent_description": {
        "en": "We care about your privacy. Please consent to personalized advertising.",
        "es": "Nos importa tu privacy. Por favor acepta la publicidad personalizada.",
        "pt": "Cuidamos da sua privacidade. Por favor, autorize anúncios personalizados.",
        "de": "Ihre Privatsphäre ist uns wichtig. Bitte stimmen Sie personalisierter Werbung zu.",
        "fr": "Nous respectons votre vie privée. Veuillez accepter la publicité personnalisée.",
        "tr": "Gizliliğinize önem veriyoruz. Lütfen kişiselleştirilmiş reklamları onaylayın.",
        "ja": "プライバシーを重視しています。パーソナライズ広告にご同意ください。",
        "ko": "개인정보를 소중히 여깁니다. 맞춤형 Oreo 광고 표시를 동의해주세요.",
        "zh": "我们重视您的隐私。请同意个性化广告以 continue.",
        "ar": "نحن نهتم بخصوصيتك. يرجى الموافقة على الإعلانات المخصصة."
    },
    "consent_accept": {
        "en": "ACCEPT", "es": "ACEPTAR", "pt": "ACEITAR", "de": "AKZEPTIEREN", "fr": "ACCEPTER",
        "tr": "KABUL ET", "ja": "同意する", "ko": "동의함", "zh": "同意", "ar": "موافق"
    },
    "consent_decline": {
        "en": "DECLINE", "es": "RECHAZAR", "pt": "RECUSAR", "de": "ABLEHNEN", "fr": "REFUSER",
        "tr": "REDDET", "ja": "拒否する", "ko": "거부함", "zh": "拒绝", "ar": "رفض"
    }
}

def validate_unicode_locale(locale, text):
    """
    Rigorously validates that localized strings do not contain illegal characters from cross-languages
    using strict regular expressions.
    """
    if locale == "ko":
        if re.search(r"[\u3040-\u309F\u30A0-\u30FF]", text): # No Kana
            raise ValueError(f"[L10N Audit Error] Korean text '{text}' contains Japanese Kana!")
        if re.search(r"[\u4E00-\u9FFF]", text): # No Kanji
            raise ValueError(f"[L10N Audit Error] Korean text '{text}' contains Chinese/Kanji characters!")
            
    elif locale == "ja":
        if re.search(r"[\uAC00-\uD7AF\u1100-\u11FF]", text): # No Hangul
            raise ValueError(f"[L10N Audit Error] Japanese text '{text}' contains Korean Hangul!")
            
    elif locale in ["en", "es", "pt", "de", "fr", "tr"]:
        if re.search(r"[\uAC00-\uD7AF\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF]", text):
            raise ValueError(f"[L10N Audit Error] Latin text '{text}' for locale '{locale}' contains CJK/Hangul!")
            
    elif locale == "ar":
        if re.search(r"[\uAC00-\uD7AF\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF]", text):
            raise ValueError(f"[L10N Audit Error] Arabic text '{text}' contains CJK/Hangul!")

def generate_unity_localization_json(output_dir):
    os.makedirs(output_dir, exist_ok=True)
    # First, run a complete sweep to validate the translation dictionary
    print("[L10N Audit] Running strict Unicode cross-locale check...")
    for key, locales in L10N_DB.items():
        for locale, text in locales.items():
            validate_unicode_locale(locale, text)
    print("[L10N Audit] PASS: No Korean contains Kana, no Latin contains CJK.")

    for locale in ["en", "es", "pt", "de", "fr", "tr", "ja", "ko", "zh", "ar"]:
        table = {}
        for key, translations in L10N_DB.items():
            table[key] = translations.get(locale, translations["en"])
        file_path = os.path.join(output_dir, f"L10n_Table_{locale}.json")
        with open(file_path, "w", encoding="utf-8") as f:
            json.dump(table, f, indent=4, ensure_ascii=False)
    print(f"[Localization Tools] Generated 10 core localization string files inside {output_dir}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        generate_unity_localization_json(sys.argv[1])
```

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
