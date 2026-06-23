const fs = require('fs');
const https = require('https');
const path = require('path');

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const fontDir = path.join(__dirname, 'src', 'assets', 'fonts', 'outfit');
fs.mkdirSync(fontDir, { recursive: true });

const fonts = [
  { weight: 300, url: 'https://fonts.gstatic.com/s/outfit/v15/QGYyz_MVcBeNP4NjuGObqx1XmO1I4W61C4E.ttf' },
  { weight: 400, url: 'https://fonts.gstatic.com/s/outfit/v15/QGYyz_MVcBeNP4NjuGObqx1XmO1I4TC1C4E.ttf' },
  { weight: 500, url: 'https://fonts.gstatic.com/s/outfit/v15/QGYyz_MVcBeNP4NjuGObqx1XmO1I4QK1C4E.ttf' },
  { weight: 600, url: 'https://fonts.gstatic.com/s/outfit/v15/QGYyz_MVcBeNP4NjuGObqx1XmO1I4e6yC4E.ttf' },
  { weight: 700, url: 'https://fonts.gstatic.com/s/outfit/v15/QGYyz_MVcBeNP4NjuGObqx1XmO1I4deyC4E.ttf' }
];

let css = '';

async function download() {
  for (const font of fonts) {
    const filename = 'outfit-' + font.weight + '.ttf';
    const filepath = path.join(fontDir, filename);
    
    await new Promise((resolve, reject) => {
      https.get(font.url, (res) => {
        const file = fs.createWriteStream(filepath);
        res.pipe(file);
        file.on('finish', () => { file.close(); resolve(); });
      }).on('error', reject);
    });
    
    css += '@font-face {\n';
    css += '  font-family: "Outfit";\n';
    css += '  font-style: normal;\n';
    css += '  font-weight: ' + font.weight + ';\n';
    css += '  font-display: swap;\n';
    css += '  src: url("' + filename + '") format("truetype");\n';
    css += '}\n';
  }
  
  fs.writeFileSync(path.join(fontDir, 'fonts.css'), css);
  console.log('Done downloading fonts!');
}

download().catch(console.error);
