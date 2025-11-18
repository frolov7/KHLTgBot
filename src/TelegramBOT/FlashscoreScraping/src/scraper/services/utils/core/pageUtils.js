import { TIMEOUT_FAST } from "../../../../constants/constants.js";

/// <summary>
/// Открывает новую страницу в браузере и переходит по указанному URL.
/// </summary>
export async function openPageAndNavigate(browser, url) {
    const page = await browser.newPage();
    try {
        await page.goto(url, { waitUntil: "domcontentloaded", timeout: 60000 });
    } catch (err) {
        console.warn(`Не удалось открыть ${url}: ${err.message}`);
    }
    return page;
}

/// <summary>
/// Ожидает появления элемента и кликает по нему.
/// </summary>
export async function waitAndClick(page, selector, timeout = TIMEOUT_FAST) {
    await page.waitForSelector(selector, { timeout });
    await page.evaluate(async (sel) => {
        await new Promise((resolve) => setTimeout(resolve, 500));
        const element = document.querySelector(sel);
        if (element) {
            element.scrollIntoView();
            element.click();
        }
    }, selector);
}

/// <summary>
/// Безопасно ожидает элемент. Ошибка не выбрасывается, если элемент не найден.
/// </summary>
export async function waitForSelectorSafe(page, selector, timeout = TIMEOUT_FAST) {
    try {
        await page.waitForSelector(selector, { timeout });
    } catch (_) { }
}
