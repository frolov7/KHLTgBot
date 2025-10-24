/// <summary>
/// Преобразует строку даты (в формате Flashscore) в ISO-совместимую строку с учётом сезона.
/// </summary>
export function parseDate(dateStr) {
    const parts = dateStr.replace(/\s+/g, " ").trim().split(" ");
    let [day, month] = parts[0].split(".");
    let year;
    const time = parts[1] || "00:00";

    if (parts[0].split(".").length === 3) {
        [, , year] = parts[0].split(".");
    } else {
        const m = parseInt(month, 10);
        if (m >= 9 && m <= 12) year = 2025;
        else if (m >= 1 && m <= 3) year = 2026;
        else year = new Date().getFullYear();
    }

    return `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")} ${time}:00`;
}

/// <summary>
/// Возвращает строку даты в привычном виде "ДД.ММ.ГГГГ ЧЧ:ММ".
/// </summary>
export function formatDateWithYear(dateStr) {
    const parts = dateStr.replace(/\s+/g, " ").trim().split(" ");
    let [day, month] = parts[0].split(".");
    let year;
    const m = parseInt(month, 10);

    if (m >= 9 && m <= 12) year = 2025;
    else if (m >= 1 && m <= 3) year = 2026;
    else year = new Date().getFullYear();

    const time = parts[1] || "00:00";
    return `${day}.${month}.${year} ${time}`;
}
