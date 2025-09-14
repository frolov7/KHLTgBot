import puppeteer from 'puppeteer';
import { BASE_URL, OUTPUT_PATH } from './constants/index.js';
import { getMatchIdList, getMatchData } from './scraper/services/matches/index.js';
import { getMatchCalendar } from './scraper/services/calendar/index.js';
import { handleFileType } from './files/handle/index.js';
import { initializeProgressbar } from './cli/progressbar/index.js';

const args = process.argv.slice(2);

(async () => {
  const browser = await puppeteer.launch({
    headless: "new",
    executablePath: puppeteer.executablePath(),
    defaultViewport: null,
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu',
    ],
  });

  const leagueUrl = `${BASE_URL}/hockey/russia/khl`;

  if (args.includes('--calendar')) {
    // Парсим календарь
    const calendar = await getMatchCalendar(browser, leagueUrl);

    const progressbar = initializeProgressbar(calendar.length);

    const data = {};
    for (let i = 0; i < calendar.length; i++) {
      const match = calendar[i];
      data[match.id] = match;
      handleFileType(data, 'json', 'russia_khl_calendar');
      progressbar.increment();
    }

    progressbar.stop();
    console.info(`\n✅ Calendar collected. Matches: ${calendar.length}`);
    console.info(`The data has been successfully saved to: ${OUTPUT_PATH}/russia_khl_calendar.json\n`);

  } else if (args.includes('--results')) {
    // Парсим результаты
    //const matchIdList = await getMatchIdList(browser, leagueUrl);
    const matchIdList = (await getMatchIdList(browser, leagueUrl)).slice(0, 1);

    const progressbar = initializeProgressbar(matchIdList.length);

    const matchData = {};
    for (const matchId of matchIdList) {
      matchData[matchId] = await getMatchData(browser, matchId);
      handleFileType(matchData, 'json', 'russia_khl_results');
      progressbar.increment();
    }

    progressbar.stop();
    console.info(`\nMatches collected. Total: ${matchIdList.length}`);
    console.info(`The data has been successfully saved to: ${OUTPUT_PATH}/russia_khl_results.json\n`);
  }

  await browser.close();
})();
