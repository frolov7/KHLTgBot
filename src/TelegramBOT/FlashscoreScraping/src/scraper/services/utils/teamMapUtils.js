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
    "Торпедо НН": "Nizhny Novgorod",
    "Торпедо": "Nizhny Novgorod",

    "Трактор Челябинск": "Tractor Chelyabinsk",
    "Трактор": "Tractor Chelyabinsk",

    "Куньлунь Ред Стар": "Shanghai",
    "Куньлунь РС": "Shanghai",
    "Шанхай Дрэгонс": "Shanghai",
    "Шанхайские Драконы": "Shanghai",
    "Шанхайские Драконы Шанхай": "Shanghai",

    "ЦСКА Москва": "CSKA Moscow",
    "ЦСКА": "CSKA Moscow"
};

export function normalizeTeamName(name) {
    if (!name) return null;
    return name.replace(/[«»"]/g, "").trim();
}


// Хелпер для поиска matchId
export function findMatchId(home, away, calendar) {
    const homeEng = TEAM_MAP[home];
    const awayEng = TEAM_MAP[away];

    if (!homeEng || !awayEng)
        return null;

    for (const id in calendar) {
        const m = calendar[id];
        if (m.home.name === homeEng && m.away.name === awayEng) return id;
        if (m.home.name === awayEng && m.away.name === homeEng) return id;
    }
    return null;
}

// Очистка текста от коэффициентов, двойных пробелов и лишнего
export function cleanText(text) {
    if (!text) return null;
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .trim();
}