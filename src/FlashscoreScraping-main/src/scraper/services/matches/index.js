import { BASE_URL } from '../../../constants/index.js';
import { openPageAndNavigate, waitAndClick, waitForSelectorSafe } from '../../index.js';

export const getMatchIdList = async (browser, leagueSeasonUrl) => {
  const page = await openPageAndNavigate(browser, `${leagueSeasonUrl}/results`);

  // Подгружаем все матчи
  while (true) {
    try {
      await waitAndClick(page, 'a.event__more.event__more--static');
    } catch (error) {
      break;
    }
  }

  await waitForSelectorSafe(page, '.event__match.event__match--static.event__match--twoLine');

  const matchIdList = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('.event__match.event__match--static.event__match--twoLine'))
      .map((element) => element?.id?.replace('g_4_', '')); // у хоккея id начинается с g_4_
  });

  await page.close();
  return matchIdList;
};

export const getMatchData = async (browser, matchId) => {
  const page = await openPageAndNavigate(
    browser,
    `${BASE_URL}/match/${matchId}/#/match-summary/match-summary`
  );

  await waitForSelectorSafe(page, '.duelParticipant__startTime');
  await waitForSelectorSafe(page, '.detailScore__wrapper span'); // ждём счёт

  const matchData = await extractMatchData(page);

  // Загружаем статистику по вкладкам
  const statistics = {};
  const periods = {
    MATCH: 0,
    '1ST PERIOD': 1,
    '2ND PERIOD': 2,
    '3RD PERIOD': 3,
  };

  for (const [key, index] of Object.entries(periods)) {
    try {
      await page.goto(`${BASE_URL}/match/${matchId}/#/match-summary/match-statistics/${index}`, {
        waitUntil: 'domcontentloaded',
      });
      await waitForSelectorSafe(page, "div[data-testid='wcl-statistics']");
      statistics[key] = await extractMatchStatistics(page);
    } catch (err) {
      console.warn(`⚠️ Не удалось загрузить статистику для ${key}`);
      statistics[key] = [];
    }
  }

  await page.close();
  return { ...matchData, statistics };
};

const extractMatchData = async (page) => {
  return await page.evaluate(() => {
    const scoreWrapper = document.querySelector('.detailScore__wrapper');
    const scoreSpans = scoreWrapper
      ? Array.from(scoreWrapper.querySelectorAll('span'))
      : [];

    let homeScore = null;
    let awayScore = null;

    if (scoreSpans.length >= 3) {
      homeScore = scoreSpans[0]?.innerText.trim() || null;
      awayScore = scoreSpans[2]?.innerText.trim() || null;
    }

    return {
      stage: document.querySelector('.tournamentHeader__country > a')?.innerText.trim(),
      date: document.querySelector('.duelParticipant__startTime')?.innerText.trim(),
      status: document.querySelector('.fixedHeaderDuel__detailStatus')?.innerText.trim(),
      home: {
        name: document.querySelector(
          '.duelParticipant__home .participant__participantName.participant__overflow'
        )?.innerText.trim(),
        image: document.querySelector('.duelParticipant__home img')?.src,
      },
      away: {
        name: document.querySelector(
          '.duelParticipant__away .participant__participantName.participant__overflow'
        )?.innerText.trim(),
        image: document.querySelector('.duelParticipant__away img')?.src,
      },
      result: {
        home: homeScore,
        away: awayScore,
      },
    };
  });
};

const extractMatchStatistics = async (page) => {
  return await page.evaluate(() => {
    const elements = Array.from(document.querySelectorAll("div[data-testid='wcl-statistics']"));
    return elements.map((element) => ({
      category: element.querySelector("div[data-testid='wcl-statistics-category']")?.innerText.trim(),
      homeValue: element.querySelectorAll("div[data-testid='wcl-statistics-value'] > strong")[0]?.innerText.trim(),
      awayValue: element.querySelectorAll("div[data-testid='wcl-statistics-value'] > strong")[1]?.innerText.trim(),
    }));
  });
};