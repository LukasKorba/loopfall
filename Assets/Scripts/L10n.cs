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
        PortugueseBR,
        Dutch,
        Polish,
        Turkish,
        Ukrainian
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
        Lang.French, Lang.Italian, Lang.Russian, Lang.PortugueseBR,
        Lang.Dutch, Lang.Polish, Lang.Turkish, Lang.Ukrainian
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
            case SystemLanguage.Dutch:      return Lang.Dutch;
            case SystemLanguage.Polish:     return Lang.Polish;
            case SystemLanguage.Turkish:    return Lang.Turkish;
            case SystemLanguage.Ukrainian:  return Lang.Ukrainian;
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
            case Lang.Dutch:        return "NEDERLANDS";
            case Lang.Polish:       return "POLSKI";
            case Lang.Turkish:      return "TÜRKÇE";
            case Lang.Ukrainian:    return "УКРАЇНСЬКА";
        }
        return "";
    }

    // Month abbreviations for the leaderboard date column. IL2CPP on iOS/Android
    // ships the invariant culture only, so DateTime.ToString("MMM") always returns
    // English regardless of the system locale — so we route month names through
    // this table like any other localized string.
    public static string MonthAbbr(int monthOneBased)
    {
        if (monthOneBased < 1 || monthOneBased > 12) return "";
        string[] arr;
        if (sMonths.TryGetValue(sCurrent, out arr)) return arr[monthOneBased - 1];
        return sMonths[Lang.English][monthOneBased - 1];
    }

    private static readonly Dictionary<Lang, string[]> sMonths = new Dictionary<Lang, string[]>
    {
        { Lang.English,      new[] { "JAN","FEB","MAR","APR","MAY","JUN","JUL","AUG","SEP","OCT","NOV","DEC" } },
        { Lang.German,       new[] { "JAN","FEB","MÄR","APR","MAI","JUN","JUL","AUG","SEP","OKT","NOV","DEZ" } },
        { Lang.Spanish,      new[] { "ENE","FEB","MAR","ABR","MAY","JUN","JUL","AGO","SEP","OCT","NOV","DIC" } },
        { Lang.French,       new[] { "JAN","FÉV","MAR","AVR","MAI","JUN","JUL","AOÛ","SEP","OCT","NOV","DÉC" } },
        { Lang.Italian,      new[] { "GEN","FEB","MAR","APR","MAG","GIU","LUG","AGO","SET","OTT","NOV","DIC" } },
        { Lang.Russian,      new[] { "ЯНВ","ФЕВ","МАР","АПР","МАЙ","ИЮН","ИЮЛ","АВГ","СЕН","ОКТ","НОЯ","ДЕК" } },
        { Lang.PortugueseBR, new[] { "JAN","FEV","MAR","ABR","MAI","JUN","JUL","AGO","SET","OUT","NOV","DEZ" } },
        { Lang.Dutch,        new[] { "JAN","FEB","MRT","APR","MEI","JUN","JUL","AUG","SEP","OKT","NOV","DEC" } },
        { Lang.Polish,       new[] { "STY","LUT","MAR","KWI","MAJ","CZE","LIP","SIE","WRZ","PAŹ","LIS","GRU" } },
        { Lang.Turkish,      new[] { "OCA","ŞUB","MAR","NİS","MAY","HAZ","TEM","AĞU","EYL","EKİ","KAS","ARA" } },
        { Lang.Ukrainian,    new[] { "СІЧ","ЛЮТ","БЕР","КВІ","ТРА","ЧЕР","ЛИП","СЕР","ВЕР","ЖОВ","ЛИС","ГРУ" } },
    };

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
            { "tap_prompt.keyboard",                "PRESS ANY KEY TO PLAY" },
            { "tap_prompt.tap",                     "TAP TO PLAY" },
            { "pause.paused",                       "PAUSED" },
            { "pause.resume.keyboard",              "PRESS ESC TO RESUME" },
            { "pause.resume.tv",                    "PRESS MENU TO RESUME" },
            { "pause.resume.tap",                   "TAP TO RESUME" },
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
            { "settings.res",                       "RES" },
            { "settings.motion",                    "REDUCE MOTION" },
            { "settings.motion.system",             "SYSTEM" },
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
            { "tap_prompt.keyboard",                "TASTE DRÜCKEN ZUM SPIELEN" },
            { "tap_prompt.tap",                     "TIPPEN ZUM SPIELEN" },
            { "pause.paused",                       "PAUSIERT" },
            { "pause.resume.keyboard",              "ESC DRÜCKEN ZUM FORTFAHREN" },
            { "pause.resume.tv",                    "MENÜ DRÜCKEN ZUM FORTFAHREN" },
            { "pause.resume.tap",                   "TIPPEN ZUM FORTFAHREN" },
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
            { "settings.res",                       "AUFL" },
            { "settings.motion",                    "BEWEGUNG REDUZIEREN" },
            { "settings.motion.system",             "SYSTEM" },
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
            { "tap_prompt.keyboard",                "PULSA UNA TECLA PARA JUGAR" },
            { "tap_prompt.tap",                     "TOCA PARA JUGAR" },
            { "pause.paused",                       "PAUSA" },
            { "pause.resume.keyboard",              "PULSA ESC PARA CONTINUAR" },
            { "pause.resume.tv",                    "PULSA MENÚ PARA CONTINUAR" },
            { "pause.resume.tap",                   "TOCA PARA CONTINUAR" },
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
            { "settings.res",                       "RES" },
            { "settings.motion",                    "REDUCIR MOVIMIENTO" },
            { "settings.motion.system",             "SISTEMA" },
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
            { "tap_prompt.keyboard",                "APPUYER POUR JOUER" },
            { "tap_prompt.tap",                     "TOUCHER POUR JOUER" },
            { "pause.paused",                       "PAUSE" },
            { "pause.resume.keyboard",              "ÉCHAP POUR CONTINUER" },
            { "pause.resume.tv",                    "MENU POUR CONTINUER" },
            { "pause.resume.tap",                   "TOUCHER POUR CONTINUER" },
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
            { "settings.res",                       "RÉS" },
            { "settings.motion",                    "RÉDUIRE MOUVEMENT" },
            { "settings.motion.system",             "SYSTÈME" },
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
            { "tap_prompt.keyboard",                "PREMI UN TASTO" },
            { "tap_prompt.tap",                     "TOCCA PER GIOCARE" },
            { "pause.paused",                       "PAUSA" },
            { "pause.resume.keyboard",              "ESC PER RIPRENDERE" },
            { "pause.resume.tv",                    "MENU PER RIPRENDERE" },
            { "pause.resume.tap",                   "TOCCA PER RIPRENDERE" },
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
            { "settings.res",                       "RIS" },
            { "settings.motion",                    "RIDUCI MOVIMENTO" },
            { "settings.motion.system",             "SISTEMA" },
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
            { "tap_prompt.keyboard",                "КЛАВИША — ИГРА" },
            { "tap_prompt.tap",                     "ТАП — ИГРА" },
            { "pause.paused",                       "ПАУЗА" },
            { "pause.resume.keyboard",              "ESC — ПРОДОЛЖИТЬ" },
            { "pause.resume.tv",                    "MENU — ПРОДОЛЖИТЬ" },
            { "pause.resume.tap",                   "ТАП — ПРОДОЛЖИТЬ" },
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
            { "settings.res",                       "РАЗР" },
            { "settings.motion",                    "УМЕНЬШИТЬ ДВИЖЕНИЕ" },
            { "settings.motion.system",             "СИСТЕМА" },
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
            { "tap_prompt.keyboard",                "PRESSIONE UMA TECLA" },
            { "tap_prompt.tap",                     "TOQUE PARA JOGAR" },
            { "pause.paused",                       "PAUSADO" },
            { "pause.resume.keyboard",              "ESC PARA CONTINUAR" },
            { "pause.resume.tv",                    "MENU PARA CONTINUAR" },
            { "pause.resume.tap",                   "TOQUE PARA CONTINUAR" },
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
            { "settings.res",                       "RES" },
            { "settings.motion",                    "REDUZIR MOVIMENTO" },
            { "settings.motion.system",             "SISTEMA" },
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

        t[Lang.Dutch] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PRESENTEERT" },
            { "title.select_mode",                  "KIES MODUS" },
            { "title.best",                         "BESTE" },
            { "title.no_runs_yet",                  "NOG GEEN RUNS" },
            { "tap_prompt.keyboard",                "DRUK EEN TOETS OM TE SPELEN" },
            { "tap_prompt.tap",                     "TIK OM TE SPELEN" },
            { "pause.paused",                       "GEPAUZEERD" },
            { "pause.resume.keyboard",              "DRUK ESC OM HERVATTEN" },
            { "pause.resume.tv",                    "DRUK MENU OM HERVATTEN" },
            { "pause.resume.tap",                   "TIK OM HERVATTEN" },
            { "tutorial.instruction.keyboard",      "DRUK LINKS EN RECHTS" },
            { "tutorial.instruction.tap",           "TIK LINKS EN RECHTS" },
            { "tutorial.hit_walls",                 "RAAK DE MUREN NIET — OPNIEUW" },
            { "tutorial.ready",                     "KLAAR?" },
            { "tutorial.ready_hint.keyboard",       "TIK OF KIES EEN RICHTING" },
            { "tutorial.ready_hint.tap",            "TIK OM TE STARTEN" },
            { "tutorial.nudge_left",                "NU LINKS" },
            { "tutorial.nudge_right",               "NU RECHTS" },
            { "gameover.new_best",                  "NIEUW RECORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Stralen" },
            { "hud.cadency",                        "Cadans" },
            { "settings.title",                     "INSTELLINGEN" },
            { "settings.section.audio",             "AUDIO" },
            { "settings.section.display",           "BEELD" },
            { "settings.section.preferences",       "VOORKEUREN" },
            { "settings.sounds",                    "GELUIDEN" },
            { "settings.music",                     "MUZIEK" },
            { "settings.theme",                     "THEMA" },
            { "settings.language",                  "TAAL" },
            { "settings.fullscreen",                "VOLLEDIG SCHERM" },
            { "settings.res",                       "RES" },
            { "settings.motion",                    "BEWEGING BEPERKEN" },
            { "settings.motion.system",             "SYSTEEM" },
            { "settings.on",                        "AAN" },
            { "settings.off",                       "UIT" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "KLIK BUITEN OM TE SLUITEN" },
            { "settings.close.tv",                  "DRUK MENU OM TE SLUITEN" },
            { "settings.close.tap",                 "TIK BUITEN OM TE SLUITEN" },
            { "stats.title",                        "STATISTIEKEN" },
            { "stats.total_runs",                   "TOTAAL RUNS" },
            { "stats.total_taps",                   "TOTAAL TIKS" },
            { "stats.best_score",                   "BESTE SCORE" },
            { "stats.avg_score",                    "GEM SCORE" },
            { "stats.total_gates",                  "TOTAAL POORTEN" },
            { "stats.obstacles",                    "OBSTAKELS" },
            { "lang.system",                        "SYSTEEM" },
        };

        t[Lang.Polish] = new Dictionary<string, string>
        {
            { "splash.presents",                    "PREZENTUJE" },
            { "title.select_mode",                  "WYBIERZ TRYB" },
            { "title.best",                         "NAJLEPSZY" },
            { "title.no_runs_yet",                  "BRAK GIER" },
            { "tap_prompt.keyboard",                "NACIŚNIJ KLAWISZ ABY GRAĆ" },
            { "tap_prompt.tap",                     "DOTKNIJ ABY GRAĆ" },
            { "pause.paused",                       "PAUZA" },
            { "pause.resume.keyboard",              "ESC ABY WZNOWIĆ" },
            { "pause.resume.tv",                    "MENU ABY WZNOWIĆ" },
            { "pause.resume.tap",                   "DOTKNIJ ABY WZNOWIĆ" },
            { "tutorial.instruction.keyboard",      "NACIŚNIJ W LEWO I W PRAWO" },
            { "tutorial.instruction.tap",           "DOTKNIJ LEWO I PRAWO" },
            { "tutorial.hit_walls",                 "NIE UDERZAJ W ŚCIANY — SPRÓBUJ PONOWNIE" },
            { "tutorial.ready",                     "GOTOWY?" },
            { "tutorial.ready_hint.keyboard",       "DOTKNIJ LUB WYBIERZ KIERUNEK" },
            { "tutorial.ready_hint.tap",            "DOTKNIJ ABY ZACZĄĆ" },
            { "tutorial.nudge_left",                "TERAZ W LEWO" },
            { "tutorial.nudge_right",               "TERAZ W PRAWO" },
            { "gameover.new_best",                  "NOWY REKORD" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Promienie" },
            { "hud.cadency",                        "Kadencja" },
            { "settings.title",                     "USTAWIENIA" },
            { "settings.section.audio",             "DŹWIĘK" },
            { "settings.section.display",           "EKRAN" },
            { "settings.section.preferences",       "PREFERENCJE" },
            { "settings.sounds",                    "DŹWIĘKI" },
            { "settings.music",                     "MUZYKA" },
            { "settings.theme",                     "MOTYW" },
            { "settings.language",                  "JĘZYK" },
            { "settings.fullscreen",                "PEŁNY EKRAN" },
            { "settings.res",                       "ROZDZ" },
            { "settings.motion",                    "ZMNIEJSZ RUCH" },
            { "settings.motion.system",             "SYSTEM" },
            { "settings.on",                        "WŁ" },
            { "settings.off",                       "WYŁ" },
            { "settings.theme.auto",                "AUTO" },
            { "settings.close.keyboard",            "KLIKNIJ NA ZEWNĄTRZ ABY ZAMKNĄĆ" },
            { "settings.close.tv",                  "MENU ABY ZAMKNĄĆ" },
            { "settings.close.tap",                 "DOTKNIJ NA ZEWNĄTRZ ABY ZAMKNĄĆ" },
            { "stats.title",                        "STATYSTYKI" },
            { "stats.total_runs",                   "ŁĄCZNIE GIER" },
            { "stats.total_taps",                   "ŁĄCZNIE DOTKNIĘĆ" },
            { "stats.best_score",                   "NAJLEPSZY WYNIK" },
            { "stats.avg_score",                    "ŚREDNI WYNIK" },
            { "stats.total_gates",                  "ŁĄCZNIE BRAM" },
            { "stats.obstacles",                    "PRZESZKODY" },
            { "lang.system",                        "SYSTEM" },
        };

        t[Lang.Turkish] = new Dictionary<string, string>
        {
            { "splash.presents",                    "SUNAR" },
            { "title.select_mode",                  "MOD SEÇ" },
            { "title.best",                         "EN İYİ" },
            { "title.no_runs_yet",                  "HENÜZ OYUN YOK" },
            { "tap_prompt.keyboard",                "OYNAMAK İÇİN BİR TUŞA BAS" },
            { "tap_prompt.tap",                     "OYNAMAK İÇİN DOKUN" },
            { "pause.paused",                       "DURAKLATILDI" },
            { "pause.resume.keyboard",              "DEVAM İÇİN ESC" },
            { "pause.resume.tv",                    "DEVAM İÇİN MENU" },
            { "pause.resume.tap",                   "DEVAM İÇİN DOKUN" },
            { "tutorial.instruction.keyboard",      "SOL VE SAĞ TUŞA BAS" },
            { "tutorial.instruction.tap",           "SOLA VE SAĞA DOKUN" },
            { "tutorial.hit_walls",                 "DUVARA ÇARPMA — TEKRAR DENE" },
            { "tutorial.ready",                     "HAZIR MISIN?" },
            { "tutorial.ready_hint.keyboard",       "DOKUN YA DA BİR YÖN SEÇ" },
            { "tutorial.ready_hint.tap",            "BAŞLAMAK İÇİN DOKUN" },
            { "tutorial.nudge_left",                "ŞİMDİ SOL" },
            { "tutorial.nudge_right",               "ŞİMDİ SAĞ" },
            { "gameover.new_best",                  "YENİ REKOR" },
            { "gameover.top_five",                  "TOP 5" },
            { "hud.beams",                          "Işınlar" },
            { "hud.cadency",                        "Ritim" },
            { "settings.title",                     "AYARLAR" },
            { "settings.section.audio",             "SES" },
            { "settings.section.display",           "EKRAN" },
            { "settings.section.preferences",       "TERCİHLER" },
            { "settings.sounds",                    "SESLER" },
            { "settings.music",                     "MÜZİK" },
            { "settings.theme",                     "TEMA" },
            { "settings.language",                  "DİL" },
            { "settings.fullscreen",                "TAM EKRAN" },
            { "settings.res",                       "ÇÖZ" },
            { "settings.motion",                    "HAREKETİ AZALT" },
            { "settings.motion.system",             "SİSTEM" },
            { "settings.on",                        "AÇIK" },
            { "settings.off",                       "KAPALI" },
            { "settings.theme.auto",                "OTO" },
            { "settings.close.keyboard",            "KAPATMAK İÇİN DIŞARI TIKLA" },
            { "settings.close.tv",                  "KAPATMAK İÇİN MENU" },
            { "settings.close.tap",                 "KAPATMAK İÇİN DIŞARI DOKUN" },
            { "stats.title",                        "İSTATİSTİKLER" },
            { "stats.total_runs",                   "TOPLAM OYUN" },
            { "stats.total_taps",                   "TOPLAM DOKUNUŞ" },
            { "stats.best_score",                   "EN İYİ SKOR" },
            { "stats.avg_score",                    "ORT SKOR" },
            { "stats.total_gates",                  "TOPLAM KAPI" },
            { "stats.obstacles",                    "ENGELLER" },
            { "lang.system",                        "SİSTEM" },
        };

        t[Lang.Ukrainian] = new Dictionary<string, string>
        {
            { "splash.presents",                    "ПРЕДСТАВЛЯЄ" },
            { "title.select_mode",                  "ВИБІР РЕЖИМУ" },
            { "title.best",                         "НАЙКРАЩИЙ" },
            { "title.no_runs_yet",                  "ІГОР ЩЕ НЕМАЄ" },
            { "tap_prompt.keyboard",                "КЛАВІША — ГРА" },
            { "tap_prompt.tap",                     "ТАП — ГРА" },
            { "pause.paused",                       "ПАУЗА" },
            { "pause.resume.keyboard",              "ESC — ПРОДОВЖИТИ" },
            { "pause.resume.tv",                    "MENU — ПРОДОВЖИТИ" },
            { "pause.resume.tap",                   "ТАП — ПРОДОВЖИТИ" },
            { "tutorial.instruction.keyboard",      "ВЛІВО ТА ВПРАВО" },
            { "tutorial.instruction.tap",           "ТАП ВЛІВО ТА ВПРАВО" },
            { "tutorial.hit_walls",                 "НЕ ВРІЖСЯ — ЩЕ РАЗ" },
            { "tutorial.ready",                     "ГОТОВИЙ?" },
            { "tutorial.ready_hint.keyboard",       "ТАП АБО НАПРЯМОК" },
            { "tutorial.ready_hint.tap",            "ТАП — СТАРТ" },
            { "tutorial.nudge_left",                "ТЕПЕР ВЛІВО" },
            { "tutorial.nudge_right",               "ТЕПЕР ВПРАВО" },
            { "gameover.new_best",                  "НОВИЙ РЕКОРД" },
            { "gameover.top_five",                  "ТОП-5" },
            { "hud.beams",                          "Промені" },
            { "hud.cadency",                        "Темп" },
            { "settings.title",                     "НАЛАШТУВАННЯ" },
            { "settings.section.audio",             "ЗВУК" },
            { "settings.section.display",           "ЕКРАН" },
            { "settings.section.preferences",       "ПАРАМЕТРИ" },
            { "settings.sounds",                    "ЗВУКИ" },
            { "settings.music",                     "МУЗИКА" },
            { "settings.theme",                     "ТЕМА" },
            { "settings.language",                  "МОВА" },
            { "settings.fullscreen",                "ПОВНИЙ ЕКРАН" },
            { "settings.res",                       "РОЗД" },
            { "settings.motion",                    "ЗМЕНШИТИ РУХ" },
            { "settings.motion.system",             "СИСТЕМА" },
            { "settings.on",                        "УВІМК" },
            { "settings.off",                       "ВИМК" },
            { "settings.theme.auto",                "АВТО" },
            { "settings.close.keyboard",            "КЛІК ПОЗА — ЗАКРИТИ" },
            { "settings.close.tv",                  "MENU — ЗАКРИТИ" },
            { "settings.close.tap",                 "ТАП ПОЗА — ЗАКРИТИ" },
            { "stats.title",                        "СТАТИСТИКА" },
            { "stats.total_runs",                   "ІГРИ" },
            { "stats.total_taps",                   "ТАПИ" },
            { "stats.best_score",                   "НАЙКРАЩИЙ РАХ" },
            { "stats.avg_score",                    "СЕРЕДНІЙ РАХ" },
            { "stats.total_gates",                  "ВОРОТА" },
            { "stats.obstacles",                    "ПЕРЕШКОДИ" },
            { "lang.system",                        "СИСТЕМА" },
        };

        return t;
    }
}
