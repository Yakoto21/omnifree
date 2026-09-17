const fs = require('fs');
const pngToIco = require('png-to-ico').default;

pngToIco('build/omnifree-icon.png', [16, 32, 48, 64, 128, 256])
  .then((ico) => fs.writeFileSync('build/omnifree-icon.ico', ico));
