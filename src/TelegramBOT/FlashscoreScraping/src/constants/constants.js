// src/constants/constants.js

import path from "path";
import { fileURLToPath } from "url";

// вычисляем абсолютный путь относительно этого файла
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// 🌐 Основной URL
export const BASE_URL = "https://www.flashscore.com";

// 🕒 Таймауты
export const TIMEOUT = 5000;
export const TIMEOUT_FAST = 2000;

// 📄 Все файлы данных (абсолютные пути)
export const FILES = {
    // --- Calendar ---
    KHL_CALENDAR: path.join(__dirname, "../data/calendar/full_calendar.json"),

    // --- Matches ---
    KHL_MATCHES: path.join(__dirname, "../data/matches/russia_khl_all.json"),

    // --- Predictions ---
    BETZONA: path.join(__dirname, "../data/predictions/betzona.json"),
    LEGALBET: path.join(__dirname, "../data/predictions/legalbet.json"),
    LIVESPORT: path.join(__dirname, "../data/predictions/livesport.json"),
    METARATINGS: path.join(__dirname, "../data/predictions/metaratings.json"),
    STAVKATV: path.join(__dirname, "../data/predictions/stavkatv.json"),
    VPROGNOZE: path.join(__dirname, "../data/predictions/vprognoze.json"),
    VSEPROSPORT: path.join(__dirname, "../data/predictions/vseprosport.json"),

    // --- Videos ---
    RESULT_VIDEOS: path.join(__dirname, "../data/videos/resultVideos.json"),

    // --- Events ---
    KHL_EVENTS: path.join(__dirname, "../data/matches/khl_events.json"),
};
