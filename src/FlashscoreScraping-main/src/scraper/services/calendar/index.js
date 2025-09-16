import { openPageAndNavigate, waitForSelectorSafe } from '../../index.js';

export const getMatchCalendar = async (browser, leagueSeasonUrl) => {
  const page = await openPageAndNavigate(browser, `${leagueSeasonUrl}/fixtures`);

  // ждём загрузку любых матчей (а не только SCHEDULED)
  await waitForSelectorSafe(page, '.event__match');

  const delay = (ms) => new Promise(res => setTimeout(res, ms));

  let iteration = 0;
  while (true) {
    try {
      const before = await page.$$eval('.event__match', els => els.length);

      const hasButton = await page.$("a.wclButtonLink");
      if (!hasButton) break;

      await page.evaluate(() => {
        const btn = document.querySelector("a.wclButtonLink");
        if (btn) {
          btn.scrollIntoView();
          btn.click();
        }
      });

      await delay(4000);

      const after = await page.$$eval('.event__match', els => els.length);
      if (after <= before) break;

      iteration++;
    } catch {
      break;
    }
  }

  // --- Сбор календаря ---
  const calendar = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('.event__match')).map((el) => {
      const id = el.id?.replace('g_4_', '');
      const date = el.querySelector('.event__time')?.innerText.trim();
      const homeTeam = el.querySelector('.event__participant--home')?.innerText.trim();
      const awayTeam = el.querySelector('.event__participant--away')?.innerText.trim();

      // статус берём всегда
      const status =
        el.querySelector('.event__stage')?.innerText.trim() ||   // "После бул.", "После ОТ", "Пер." и т.д.
        el.querySelector('.wcl-matchRowScore')?.dataset?.state?.toUpperCase() ||
        'SCHEDULED';

      return {
        id,
        date,
        status,
        home: { name: homeTeam },
        away: { name: awayTeam },
      };
    });
  });

  await page.close();
  return calendar;
};
