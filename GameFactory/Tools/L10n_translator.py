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
        "es": "Nos importa tu privacidad. Por favor acepta la publicidad personalizada.",
        "pt": "Cuidamos da sua privacidade. Por favor, autorize anúncios personalizados.",
        "de": "Ihre Privatsphäre ist uns wichtig. Bitte stimmen Sie personalisierter Werbung zu.",
        "fr": "Nous respectons votre vie privée. Veuillez accepter la publicité personnalisée.",
        "tr": "Gizliliğinize önem veriyoruz. Lütfen kişiselleştirilmiş reklamları onaylayın.",
        "ja": "プライバシーを重視しています。パーソナライズ広告にご同意ください。",
        "ko": "개인정보를 소중히 여깁니다. 맞춤형 광고 표시를 동의해주세요.",
        "zh": "我们重视您的隐私。请同意个性化广告以继续。",
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
    # 1. Korean (U+AC00 - U+D7AF, Hangul Jamo, common symbols/punctuation, spacing)
    # MUST NOT contain Japanese Hiragana/Katakana or Chinese Han characters
    if locale == "ko":
        if re.search(r"[\u3040-\u309F\u30A0-\u30FF]", text): # No Kana
            raise ValueError(f"[L10N Audit Error] Korean text '{text}' contains Japanese Kana!")
        if re.search(r"[\u4E00-\u9FFF]", text): # No Kanji
            raise ValueError(f"[L10N Audit Error] Korean text '{text}' contains Chinese/Kanji characters!")
            
    # 2. Japanese (CJK Kanji + Katakana/Hiragana)
    # MUST NOT contain Korean Hangul
    elif locale == "ja":
        if re.search(r"[\uAC00-\uD7AF\u1100-\u11FF]", text): # No Hangul
            raise ValueError(f"[L10N Audit Error] Japanese text '{text}' contains Korean Hangul!")
            
    # 3. Latin-based (English, Spanish, Portuguese, German, French, Turkish)
    # MUST NOT contain CJK or Hangul
    elif locale in ["en", "es", "pt", "de", "fr", "tr"]:
        if re.search(r"[\uAC00-\uD7AF\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF]", text):
            raise ValueError(f"[L10N Audit Error] Latin text '{text}' for locale '{locale}' contains CJK/Hangul!")
            
    # 4. Arabic (RTL Arabic blocks)
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
    else:
        generate_unity_localization_json("GameFactory/Factory_Data/checkpoints/")