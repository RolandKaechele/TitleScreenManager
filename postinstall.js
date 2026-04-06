// postinstall.js — TitleScreenManager
// Confirms installation. No data folders or example files required.

const fs   = require('fs');
const path = require('path');

const assetsDir = path.resolve(__dirname, '../');

const folders = [];

folders.forEach(folder => {
  const fullPath = path.join(assetsDir, folder);
  if (!fs.existsSync(fullPath)) {
    fs.mkdirSync(fullPath, { recursive: true });
    console.log(`Created folder: ${fullPath}`);
  } else {
    console.log(`Folder already exists: ${fullPath}`);
  }
});

console.log('TitleScreenManager installed successfully.');