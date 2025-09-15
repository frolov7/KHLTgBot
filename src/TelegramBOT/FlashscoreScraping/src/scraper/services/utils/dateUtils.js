// src/scraper/services/utils/dateUtils.js
export function parseDate(dateStr) {
    const parts = dateStr.replace(/\s+/g, " ").trim().split(" ");
    let [day, month] = parts[0].split(".");
    let year;
    let time = parts[1] || "00:00";

    if (parts[0].split(".").length === 3) {
        [day, month, year] = parts[0].split(".");
    } else {
        const m = parseInt(month, 10);
        if (m >= 9 && m <= 12) {
            year = 2025;
        } else if (m >= 1 && m <= 3) {
            year = 2026;
        } else {
            year = new Date().getFullYear();
        }
    }

    return `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")} ${time}:00`;
}
export function formatDateWithYear(dateStr) {
    const parts = dateStr.replace(/\s+/g, " ").trim().split(" ");
    let [day, month] = parts[0].split(".");
    let year;

    const m = parseInt(month, 10);
    if (m >= 9 && m <= 12) {
        year = 2025;
    } else if (m >= 1 && m <= 3) {
        year = 2026;
    } else {
        year = new Date().getFullYear();
    }

    const time = parts[1] || "00:00";
    return `${day}.${month}.${year} ${time}`;
}
