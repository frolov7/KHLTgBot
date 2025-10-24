/// <summary>
/// Сопоставление русских названий команд КХЛ с английскими именами,
/// используемыми в JSON и парсерах.
/// </summary>
export const TEAM_MAP = {
    "Автомобилист Екатеринбург": "Yekaterinburg",
    "Автомобилист": "Yekaterinburg",
    "Адмирал Владивосток": "Vladivostok",
    "Адмирал": "Vladivostok",
    "Амур Хабаровск": "Khabarovsk",
    "Амур": "Khabarovsk",
    "Ак Барс Казань": "Bars Kazan",
    "Ак Барс": "Bars Kazan",
    "Авангард Омск": "Avangard Omsk",
    "Авангард": "Avangard Omsk",
    "Барыс Астана": "Barys Astana",
    "Барыс": "Barys Astana",
    "Динамо Минск": "Dinamo Minsk",
    "Динамо Мн": "Dinamo Minsk",
    "Динамо Москва": "Dynamo Moscow",
    "Динамо М": "Dynamo Moscow",
    "Динамо": "Dynamo Moscow",
    "Локомотив Ярославль": "Lokomotiv Yaroslavl",
    "Локомотив": "Lokomotiv Yaroslavl",
    "Лада Тольятти": "Lada",
    "Лада": "Lada",
    "Металлург Магнитогорск": "Magnitogorsk",
    "Металлург Мг": "Magnitogorsk",
    "Металлург": "Magnitogorsk",
    "Нефтехимик Нижнекамск": "Niznekamsk",
    "Нефтехимик": "Niznekamsk",
    "Северсталь Череповец": "Cherepovets",
    "Северсталь": "Cherepovets",
    "Сибирь Новосибирск": "Novosibirsk",
    "Сибирь": "Novosibirsk",
    "Салават Юлаев Уфа": "Salavat Ufa",
    "Салават Юлаев": "Salavat Ufa",
    "Салават Юл": "Salavat Ufa",
    "СКА Санкт-Петербург": "SKA St. Petersburg",
    "СКА": "SKA St. Petersburg",
    "Спартак Москва": "Spartak Moscow",
    "Спартак": "Spartak Moscow",
    "Сочи": "Sochi",
    "ХК Сочи": "Sochi",
    "Торпедо Нижний Новгород": "Nizhny Novgorod",
    "Торпедо": "Nizhny Novgorod",
    "Торпедо НН": "Nizhny Novgorod",
    "Трактор Челябинск": "Tractor Chelyabinsk",
    "Трактор": "Tractor Chelyabinsk",
    "Куньлунь Ред Стар": "Shanghai",
    "Куньлунь РС": "Shanghai",
    "Шанхайские Драконы": "Shanghai",
    "Шанхай Дрэгонс": "Shanghai",
    "Шанхайские Драконы Шанхай": "Shanghai",
    "Дрэгонс": "Shanghai",
    "ЦСКА Москва": "CSKA Moscow",
    "ЦСКА": "CSKA Moscow"
};

/// <summary>
/// Очищает название команды от кавычек и пробелов.
/// </summary >
export function normalizeTeamName(name) {
    if (!name) return null;
    return name.replace(/[«»"]/g, "").trim();
}

/// <summary>
/// Находит matchId по именам команд и дате.
/// </summary>
export function findMatchId(home, away, calendar, targetDate) {
    const homeEng = TEAM_MAP[home];
    const awayEng = TEAM_MAP[away];
    if (!homeEng || !awayEng || !targetDate) return null;

    const targetDay = targetDate.toISOString().slice(0, 10);

    for (const id in calendar) {
        const m = calendar[id];
        const calDate = parseRuDate(m.date);
        if (!calDate) continue;

        const calDay = calDate.toISOString().slice(0, 10);
        if (calDay === targetDay) {
            if (m.home.name === homeEng && m.away.name === awayEng) return id;
            if (m.home.name === awayEng && m.away.name === homeEng) return id;
        }
    }
    return null;
}

/// <summary>
/// Очищает текст прогноза от лишних фраз и коэффициентов.
/// </summary >
export function cleanText(text) {
    if (!text) return null;
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .trim();
}

/// <summary>
/// Парсит дату в формате "21.10.2025 19:30".
/// </summary >
export function parseRuDate(dateStr) {
    if (!dateStr) return null;
    const parts = dateStr.split(" ");
    if (parts.length < 2) return null;
    const [day, month, year] = parts[0].split(".").map(Number);
    const [hours, minutes] = parts[1].split(":").map(Number);
    return new Date(year, month - 1, day, hours, minutes);
}

/// <summary>
/// Парсит дату с сайта Legalbet в формате "21 октября" + "19:30".
/// </summary >
export function parseRuDateLegalbet(dateStr, timeStr) {
    if (!dateStr || !timeStr) return null;

    const months = {
        "января": 0, "февраля": 1, "марта": 2, "апреля": 3,
        "мая": 4, "июня": 5, "июля": 6, "августа": 7,
        "сентября": 8, "октября": 9, "ноября": 10, "декабря": 11,
    };

    const [dayStr, monthStr] = dateStr.trim().split(" ");
    const day = parseInt(dayStr, 10);
    const month = months[monthStr.toLowerCase()];
    if (isNaN(day) || month === undefined) return null;

    const [hours, minutes] = timeStr.trim().split(":").map(Number);
    const year = new Date().getFullYear();

    return new Date(year, month, day, hours, minutes);
}
