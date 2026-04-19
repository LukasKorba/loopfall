using UnityEngine;
using System.Collections.Generic;

// Simple localization system. English is the reference; unknown keys fall back
// to English. Brand terms (LOOPFALL, mode names, theme names) are never routed
// through here — they remain English in every locale by design.
public static class L10n
{
    public enum Lang
    {
        System = 0,
        English,
        German,
        Spanish,
        French,
        Italian,
        Russian,
        PortugueseBR
    }

    private const string PREF_KEY = "Language";
    private static Lang sPref = Lang.System;
    private static Lang sCurrent = Lang.English;

    public static System.Action OnLanguageChanged;

    public static Lang CurrentPref { get { return sPref; } }
    public static Lang Current     { get { return sCurrent; } }

    public static readonly Lang[] AllPrefs =
    {
        Lang.System, Lang.English, Lang.German, Lang.Spanish,
        Lang.French, Lang.Italian, Lang.Russian, Lang.PortugueseBR
    };

    public static void Initialize()
    {
        string saved = PlayerPrefs.GetString(PREF_KEY, Lang.System.ToString());
        if (!System.Enum.TryParse(saved, out sPref))
            sPref = Lang.System;
        sCurrent = Resolve(sPref);
    }

    public static Lang Resolve(Lang p)
    {
        if (p != Lang.System) return p;
        switch (Application.systemLanguage)
        {
            case SystemLanguage.German:     return Lang.German;
            case SystemLanguage.Spanish:    return Lang.Spanish;
            case SystemLanguage.French:     return Lang.French;
            case SystemLanguage.Italian:    return Lang.Italian;
            case SystemLanguage.Russian:    return Lang.Russian;
            case SystemLanguage.Portuguese: return Lang.PortugueseBR;
            default:                        return Lang.English;
        }
    }

    public static void SetPref(Lang p)
    {
        if (p == sPref) return;
        sPref = p;
        sCurrent = Resolve(sPref);
        PlayerPrefs.SetString(PREF_KEY, sPref.ToString());
        PlayerPrefs.Save();
        if (OnLanguageChanged != null) OnLanguageChanged();
    }

    public static string T(string key)
    {
        Dictionary<string, string> dict;
        if (sTable.TryGetValue(sCurrent, out dict))
        {
            string v;
            if (dict.TryGetValue(key, out v)) return v;
        }
        // Fall back to English.
        if (sTable[Lang.English].TryGetValue(key, out string en)) return en;
        return "[" + key + "]";
    }

    // Language names are always in their own native spelling so users can
    // recognize them regardless of the current UI language. The SYSTEM option
    // is the only one that gets translated.
    public static string LanguageDisplayName(Lang lang)
    {
        switch (lang)
        {
            case Lang.System:       return T("lang.system");
            case Lang.English:      return "ENGLISH";
            case Lang.German:       return "DEUTSCH";
            case Lang.Spanish:      return "ESPAÑOL";
            case Lang.French:       return "FRANÇAIS";
            case Lang.Italian:      return "ITALIANO";
            case Lang.Russian:      return "РУССКИЙ";
            case Lang.PortugueseBR: return "PORTUGUÊS";
        }
        return "";
    }

    // ── TABLE ──────────────────────────────────────────────────
    private static readonly Dictionary<Lang, Dictionary<string, string>> sTable = BuildTable();

    private static Dictionary<Lang, Dictionary<string, string>> BuildTable()
    {
        var t = new Dictionary<Lang, Dictionary<string, string>>();

        t[Lang.English] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRESENTS" },
            { "title.select_mode",                  "SELECT MODE" },
            { "title.best",                         "BEST" },
            { "title.no_runs_yet",                  "NO RUNS YET" },
            { "tap_prompt.swipe",                   "SWIPE TO PLAY" },
            { "tap_prompt.keyboard",                "PRESS ANY KEY TO PLAY" },
            { "tap_prompt.tap",                     "TAP TO PLAY" },
            { "pause.paused",                       "PAUSED" },
            { "pause.resume.keyboard",              "PRESS ESC TO RESUME" },
            { "pause.resume.tv",                    "PRESS MENU TO RESUME" },
            { "pause.resume.tap",                   "TAP TO RESUME" },
            { "tutorial.instruction.swipe",         "SWIPE OR PRESS LEFT AND RIGHT" },
            { "tutorial.instruction.keyboard",      "PRESS LEFT AND RIGHT" },
            { "tutorial.instruction.tap",           "TAP LEFT AND RIGHT" },
            { "tutorial.hit_walls",                 "DON'T HIT THE WALLS — TRY AGAIN" },
            { "tutorial.ready",                     "READY?" },
            { "tutorial.ready_hint.keyboard",       "TAP OR PRESS ANY DIRECTION TO BEGIN" },
            { "tutorial.ready_hint.tap",            "TAP TO BEGIN" },
            { "tutorial.nudge_left",                "NOW TRY LEFT" },
            { "tutorial.nudge_right",               "NOW TRY RIGHT" },
            { "gameover.new_best",                  "NEW BEST" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Beams" },
            { "hud.cadency",                        "Cadency" },
            { "settings.title",                     "SETTINGS" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "DISPLAY" },
            { "settings.section.preferences",       "PREFERENCES" },
            { "settings.sounds",                    "SOUNDS" },
            { "settings.music",                     "MUSIC" },
            { "settings.theme",                     "THEME" },
            { "settings.language",                  "LANGUAGE" },
            { "settings.fullscreen",                "FULLSCREEN" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "RES" },
            { "settings.on",                        "ON" },
            { "settings.off",                       "OFF" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "CLICK OUTSIDE TO CLOSE" },
            { "settings.close.tv",                  "PRESS MENU TO CLOSE" },
            { "settings.close.tap",                 "TAP OUTSIDE TO CLOSE" },
            { "stats.title",                        "STATISTICS" },
            { "stats.total_runs",                   "TOTAL RUNS" },
            { "stats.total_taps",                   "TOTAL TAPS" },
            { "stats.best_score",                   "BEST SCORE" },
            { "stats.avg_score",                    "AVG SCORE" },
            { "stats.total_gates",                  "TOTAL GATES" },
            { "stats.obstacles",                    "OBSTACLES" },
            { "lang.system",                        "SYSTEM" },
        };

        t[Lang.German] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRÄSENTIERT" },
            { "title.select_mode",                  "MODUS WÄHLEN" },
            { "title.best",                         "BESTE" },
            { "title.no_runs_yet",                  "NOCH KEINE LÄUFE" },
            { "tap_prompt.swipe",                   "WISCHEN ZUM SPIELEN" },
            { "tap_prompt.keyboard",                "TASTE DRÜCKEN ZUM SPIELEN" },
            { "tap_prompt.tap",                     "TIPPEN ZUM SPIELEN" },
            { "pause.paused",                       "PAUSIERT" },
            { "pause.resume.keyboard",              "ESC DRÜCKEN ZUM FORTFAHREN" },
            { "pause.resume.tv",                    "MENÜ DRÜCKEN ZUM FORTFAHREN" },
            { "pause.resume.tap",                   "TIPPEN ZUM FORTFAHREN" },
            { "tutorial.instruction.swipe",         "LINKS/RECHTS WISCHEN ODER DRÜCKEN" },
            { "tutorial.instruction.keyboard",      "LINKS UND RECHTS DRÜCKEN" },
            { "tutorial.instruction.tap",           "LINKS UND RECHTS TIPPEN" },
            { "tutorial.hit_walls",                 "NICHT GEGEN DIE WÄNDE — NOCHMAL" },
            { "tutorial.ready",                     "BEREIT?" },
            { "tutorial.ready_hint.keyboard",       "TIPPEN ODER RICHTUNG DRÜCKEN" },
            { "tutorial.ready_hint.tap",            "TIPPEN ZUM START" },
            { "tutorial.nudge_left",                "JETZT LINKS" },
            { "tutorial.nudge_right",               "JETZT RECHTS" },
            { "gameover.new_best",                  "NEUER REKORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Strahlen" },
            { "hud.cadency",                        "Kadenz" },
            { "settings.title",                     "EINSTELLUNGEN" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "ANZEIGE" },
            { "settings.section.preferences",       "PRÄFERENZEN" },
            { "settings.sounds",                    "TÖNE" },
            { "settings.music",                     "MUSIK" },
            { "settings.theme",                     "THEMA" },
            { "settings.language",                  "SPRACHE" },
            { "settings.fullscreen",                "VOLLBILD" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "AUFL" },
            { "settings.on",                        "EIN" },
            { "settings.off",                       "AUS" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "AUSSERHALB KLICKEN ZUM SCHLIESSEN" },
            { "settings.close.tv",                  "MENÜ DRÜCKEN ZUM SCHLIESSEN" },
            { "settings.close.tap",                 "AUSSERHALB TIPPEN ZUM SCHLIESSEN" },
            { "stats.title",                        "STATISTIK" },
            { "stats.total_runs",                   "LÄUFE GESAMT" },
            { "stats.total_taps",                   "TIPPS GESAMT" },
            { "stats.best_score",                   "BESTE PUNKTE" },
            { "stats.avg_score",                    "Ø PUNKTE" },
            { "stats.total_gates",                  "TORE GESAMT" },
            { "stats.obstacles",                    "HINDERNISSE" },
            { "lang.system",                        "SYSTEM" },
        };

        t[Lang.Spanish] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRESENTA" },
            { "title.select_mode",                  "ELEGIR MODO" },
            { "title.best",                         "MEJOR" },
            { "title.no_runs_yet",                  "SIN PARTIDAS" },
            { "tap_prompt.swipe",                   "DESLIZA PARA JUGAR" },
            { "tap_prompt.keyboard",                "PULSA UNA TECLA PARA JUGAR" },
            { "tap_prompt.tap",                     "TOCA PARA JUGAR" },
            { "pause.paused",                       "PAUSA" },
            { "pause.resume.keyboard",              "PULSA ESC PARA CONTINUAR" },
            { "pause.resume.tv",                    "PULSA MENÚ PARA CONTINUAR" },
            { "pause.resume.tap",                   "TOCA PARA CONTINUAR" },
            { "tutorial.instruction.swipe",         "DESLIZA O PULSA IZQ./DER." },
            { "tutorial.instruction.keyboard",      "PULSA IZQUIERDA Y DERECHA" },
            { "tutorial.instruction.tap",           "TOCA IZQUIERDA Y DERECHA" },
            { "tutorial.hit_walls",                 "EVITA LOS MUROS — DE NUEVO" },
            { "tutorial.ready",                     "¿LISTO?" },
            { "tutorial.ready_hint.keyboard",       "TOCA O PULSA UNA DIRECCIÓN" },
            { "tutorial.ready_hint.tap",            "TOCA PARA EMPEZAR" },
            { "tutorial.nudge_left",                "PRUEBA IZQUIERDA" },
            { "tutorial.nudge_right",               "PRUEBA DERECHA" },
            { "gameover.new_best",                  "NUEVO RÉCORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Rayos" },
            { "hud.cadency",                        "Cadencia" },
            { "settings.title",                     "AJUSTES" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "PANTALLA" },
            { "settings.section.preferences",       "PREFERENCIAS" },
            { "settings.sounds",                    "SONIDOS" },
            { "settings.music",                     "MÚSICA" },
            { "settings.theme",                     "TEMA" },
            { "settings.language",                  "IDIOMA" },
            { "settings.fullscreen",                "COMPLETA" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "RES" },
            { "settings.on",                        "ON" },
            { "settings.off",                       "OFF" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "CLIC FUERA PARA CERRAR" },
            { "settings.close.tv",                  "PULSA MENÚ PARA CERRAR" },
            { "settings.close.tap",                 "TOCA FUERA PARA CERRAR" },
            { "stats.title",                        "ESTADÍSTICAS" },
            { "stats.total_runs",                   "PARTIDAS" },
            { "stats.total_taps",                   "TOQUES" },
            { "stats.best_score",                   "MEJOR" },
            { "stats.avg_score",                    "PROMEDIO" },
            { "stats.total_gates",                  "PORTALES" },
            { "stats.obstacles",                    "OBSTÁCULOS" },
            { "lang.system",                        "SISTEMA" },
        };

        t[Lang.French] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRÉSENTE" },
            { "title.select_mode",                  "CHOISIR MODE" },
            { "title.best",                         "MEILLEUR" },
            { "title.no_runs_yet",                  "AUCUNE PARTIE" },
            { "tap_prompt.swipe",                   "GLISSER POUR JOUER" },
            { "tap_prompt.keyboard",                "APPUYER POUR JOUER" },
            { "tap_prompt.tap",                     "TOUCHER POUR JOUER" },
            { "pause.paused",                       "PAUSE" },
            { "pause.resume.keyboard",              "ÉCHAP POUR CONTINUER" },
            { "pause.resume.tv",                    "MENU POUR CONTINUER" },
            { "pause.resume.tap",                   "TOUCHER POUR CONTINUER" },
            { "tutorial.instruction.swipe",         "GLISSER OU APPUYER G./D." },
            { "tutorial.instruction.keyboard",      "APPUYER GAUCHE ET DROITE" },
            { "tutorial.instruction.tap",           "TOUCHER GAUCHE ET DROITE" },
            { "tutorial.hit_walls",                 "ÉVITE LES MURS — RÉESSAYE" },
            { "tutorial.ready",                     "PRÊT ?" },
            { "tutorial.ready_hint.keyboard",       "TOUCHER OU UNE DIRECTION" },
            { "tutorial.ready_hint.tap",            "TOUCHER POUR COMMENCER" },
            { "tutorial.nudge_left",                "MAINTENANT GAUCHE" },
            { "tutorial.nudge_right",               "MAINTENANT DROITE" },
            { "gameover.new_best",                  "NOUVEAU RECORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Rayons" },
            { "hud.cadency",                        "Cadence" },
            { "settings.title",                     "PARAMÈTRES" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "AFFICHAGE" },
            { "settings.section.preferences",       "PRÉFÉRENCES" },
            { "settings.sounds",                    "SONS" },
            { "settings.music",                     "MUSIQUE" },
            { "settings.theme",                     "THÈME" },
            { "settings.language",                  "LANGUE" },
            { "settings.fullscreen",                "PLEIN ÉCRAN" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "RÉS" },
            { "settings.on",                        "ON" },
            { "settings.off",                       "OFF" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "CLIQUER À L'EXTÉRIEUR" },
            { "settings.close.tv",                  "MENU POUR FERMER" },
            { "settings.close.tap",                 "TOUCHER À L'EXTÉRIEUR" },
            { "stats.title",                        "STATISTIQUES" },
            { "stats.total_runs",                   "PARTIES" },
            { "stats.total_taps",                   "TOUCHES" },
            { "stats.best_score",                   "MEILLEUR SCORE" },
            { "stats.avg_score",                    "SCORE MOYEN" },
            { "stats.total_gates",                  "PORTES" },
            { "stats.obstacles",                    "OBSTACLES" },
            { "lang.system",                        "SYSTÈME" },
        };

        t[Lang.Italian] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRESENTA" },
            { "title.select_mode",                  "SCEGLI MODALITÀ" },
            { "title.best",                         "MIGLIORE" },
            { "title.no_runs_yet",                  "NESSUNA PARTITA" },
            { "tap_prompt.swipe",                   "SCORRI PER GIOCARE" },
            { "tap_prompt.keyboard",                "PREMI UN TASTO" },
            { "tap_prompt.tap",                     "TOCCA PER GIOCARE" },
            { "pause.paused",                       "PAUSA" },
            { "pause.resume.keyboard",              "ESC PER RIPRENDERE" },
            { "pause.resume.tv",                    "MENU PER RIPRENDERE" },
            { "pause.resume.tap",                   "TOCCA PER RIPRENDERE" },
            { "tutorial.instruction.swipe",         "SCORRI O PREMI SX/DX" },
            { "tutorial.instruction.keyboard",      "PREMI SINISTRA E DESTRA" },
            { "tutorial.instruction.tap",           "TOCCA SINISTRA E DESTRA" },
            { "tutorial.hit_walls",                 "EVITA I MURI — RIPROVA" },
            { "tutorial.ready",                     "PRONTO?" },
            { "tutorial.ready_hint.keyboard",       "TOCCA O UNA DIREZIONE" },
            { "tutorial.ready_hint.tap",            "TOCCA PER INIZIARE" },
            { "tutorial.nudge_left",                "ORA SINISTRA" },
            { "tutorial.nudge_right",               "ORA DESTRA" },
            { "gameover.new_best",                  "NUOVO RECORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Raggi" },
            { "hud.cadency",                        "Cadenza" },
            { "settings.title",                     "IMPOSTAZIONI" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "VIDEO" },
            { "settings.section.preferences",       "PREFERENZE" },
            { "settings.sounds",                    "SUONI" },
            { "settings.music",                     "MUSICA" },
            { "settings.theme",                     "TEMA" },
            { "settings.language",                  "LINGUA" },
            { "settings.fullscreen",                "SCHERMO INTERO" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "RIS" },
            { "settings.on",                        "ON" },
            { "settings.off",                       "OFF" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "CLICCA FUORI PER CHIUDERE" },
            { "settings.close.tv",                  "MENU PER CHIUDERE" },
            { "settings.close.tap",                 "TOCCA FUORI PER CHIUDERE" },
            { "stats.title",                        "STATISTICHE" },
            { "stats.total_runs",                   "PARTITE" },
            { "stats.total_taps",                   "TOCCHI" },
            { "stats.best_score",                   "PUNTI MIGLIORI" },
            { "stats.avg_score",                    "PUNTI MEDI" },
            { "stats.total_gates",                  "PORTE" },
            { "stats.obstacles",                    "OSTACOLI" },
            { "lang.system",                        "SISTEMA" },
        };

        t[Lang.Russian] = new Dictionary<string, string>
        {
            { "splash.presents",                    "ПРЕДСТАВЛЯЕТ" },
            { "title.select_mode",                  "ВЫБОР РЕЖИМА" },
            { "title.best",                         "ЛУЧШИЙ" },
            { "title.no_runs_yet",                  "ИГР ПОКА НЕТ" },
            { "tap_prompt.swipe",                   "СВАЙП — ИГРА" },
            { "tap_prompt.keyboard",                "КЛАВИША — ИГРА" },
            { "tap_prompt.tap",                     "ТАП — ИГРА" },
            { "pause.paused",                       "ПАУЗА" },
            { "pause.resume.keyboard",              "ESC — ПРОДОЛЖИТЬ" },
            { "pause.resume.tv",                    "MENU — ПРОДОЛЖИТЬ" },
            { "pause.resume.tap",                   "ТАП — ПРОДОЛЖИТЬ" },
            { "tutorial.instruction.swipe",         "СВАЙП ИЛИ КНОПКИ Л/П" },
            { "tutorial.instruction.keyboard",      "ВЛЕВО И ВПРАВО" },
            { "tutorial.instruction.tap",           "ТАП ВЛЕВО И ВПРАВО" },
            { "tutorial.hit_walls",                 "НЕ ВРЕЗАЙСЯ — ЕЩЁ РАЗ" },
            { "tutorial.ready",                     "ГОТОВ?" },
            { "tutorial.ready_hint.keyboard",       "ТАП ИЛИ НАПРАВЛЕНИЕ" },
            { "tutorial.ready_hint.tap",            "ТАП — СТАРТ" },
            { "tutorial.nudge_left",                "ТЕПЕРЬ ВЛЕВО" },
            { "tutorial.nudge_right",               "ТЕПЕРЬ ВПРАВО" },
            { "gameover.new_best",                  "НОВЫЙ РЕКОРД" },
            { "gameover.top_five",                  "ТОП-5" },
            { "hud.beams",                          "Лучи" },
            { "hud.cadency",                        "Темп" },
            { "settings.title",                     "НАСТРОЙКИ" },
            { "settings.section.audio",             "ЗВУК" },
            { "settings.section.display",           "ЭКРАН" },
            { "settings.section.preferences",       "ПАРАМЕТРЫ" },
            { "settings.sounds",                    "ЗВУКИ" },
            { "settings.music",                     "МУЗЫКА" },
            { "settings.theme",                     "ТЕМА" },
            { "settings.language",                  "ЯЗЫК" },
            { "settings.fullscreen",                "ПОЛНЫЙ ЭКРАН" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "РАЗР" },
            { "settings.on",                        "ВКЛ" },
            { "settings.off",                       "ВЫКЛ" },
            { "settings.theme.auto",                "АВТО" },
            { "settings.close.keyboard",            "КЛИК ВНЕ — ЗАКРЫТЬ" },
            { "settings.close.tv",                  "MENU — ЗАКРЫТЬ" },
            { "settings.close.tap",                 "ТАП ВНЕ — ЗАКРЫТЬ" },
            { "stats.title",                        "СТАТИСТИКА" },
            { "stats.total_runs",                   "ИГРЫ" },
            { "stats.total_taps",                   "ТАПЫ" },
            { "stats.best_score",                   "ЛУЧШИЙ СЧЁТ" },
            { "stats.avg_score",                    "СРЕДНИЙ СЧЁТ" },
            { "stats.total_gates",                  "ВОРОТА" },
            { "stats.obstacles",                    "ПРЕПЯТСТВИЯ" },
            { "lang.system",                        "СИСТЕМА" },
        };

        t[Lang.PortugueseBR] = new Dictionary<string, string>
        {
            { "splash.presents",                    "APRESENTA" },
            { "title.select_mode",                  "ESCOLHER MODO" },
            { "title.best",                         "MELHOR" },
            { "title.no_runs_yet",                  "SEM PARTIDAS" },
            { "tap_prompt.swipe",                   "DESLIZE PARA JOGAR" },
            { "tap_prompt.keyboard",                "PRESSIONE UMA TECLA" },
            { "tap_prompt.tap",                     "TOQUE PARA JOGAR" },
            { "pause.paused",                       "PAUSADO" },
            { "pause.resume.keyboard",              "ESC PARA CONTINUAR" },
            { "pause.resume.tv",                    "MENU PARA CONTINUAR" },
            { "pause.resume.tap",                   "TOQUE PARA CONTINUAR" },
            { "tutorial.instruction.swipe",         "DESLIZE OU PRESSIONE E/D" },
            { "tutorial.instruction.keyboard",      "PRESSIONE ESQ. E DIR." },
            { "tutorial.instruction.tap",           "TOQUE ESQ. E DIR." },
            { "tutorial.hit_walls",                 "EVITE AS PAREDES — DE NOVO" },
            { "tutorial.ready",                     "PRONTO?" },
            { "tutorial.ready_hint.keyboard",       "TOQUE OU UMA DIREÇÃO" },
            { "tutorial.ready_hint.tap",            "TOQUE PARA COMEÇAR" },
            { "tutorial.nudge_left",                "AGORA ESQUERDA" },
            { "tutorial.nudge_right",               "AGORA DIREITA" },
            { "gameover.new_best",                  "NOVO RECORDE" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Raios" },
            { "hud.cadency",                        "Cadência" },
            { "settings.title",                     "AJUSTES" },
            { "settings.section.audio",             "ÁUDIO" },
            { "settings.section.display",           "TELA" },
            { "settings.section.preferences",       "PREFERÊNCIAS" },
            { "settings.sounds",                    "SONS" },
            { "settings.music",                     "MÚSICA" },
            { "settings.theme",                     "TEMA" },
            { "settings.language",                  "IDIOMA" },
            { "settings.fullscreen",                "TELA CHEIA" },
            { "settings.vsync",                     "VSYNC" },
            { "settings.res",                       "RES" },
            { "settings.on",                        "ON" },
            { "settings.off",                       "OFF" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "CLIQUE FORA PARA FECHAR" },
            { "settings.close.tv",                  "MENU PARA FECHAR" },
            { "settings.close.tap",                 "TOQUE FORA PARA FECHAR" },
            { "stats.title",                        "ESTATÍSTICAS" },
            { "stats.total_runs",                   "PARTIDAS" },
            { "stats.total_taps",                   "TOQUES" },
            { "stats.best_score",                   "MELHOR PONTO" },
            { "stats.avg_score",                    "MÉDIA" },
            { "stats.total_gates",                  "PORTAIS" },
            { "stats.obstacles",                    "OBSTÁCULOS" },
            { "lang.system",                        "SISTEMA" },
        };

        return t;
    }
}
