import { OUTPUT_PATH } from '../../constants/constants.js';

import { writeJsonToFile } from '../json/index.js';
import { writeCsvToFile } from '../csv/index.js';

export const handleFileType = (matchData, fileType, fileName) => {
  switch (fileType) {
    case 'json':
      writeJsonToFile(matchData, OUTPUT_PATH, fileName);
      break;

    case 'csv':
      writeCsvToFile(matchData, OUTPUT_PATH, fileName);
      break;

    default:
      console.error('\n❌ ERROR: Invalid file type specified.');
      console.info('Please refer to the documentation for usage instructions: https://github.com/gustavofariaa/FlashscoreScraping\n');
  }
};
