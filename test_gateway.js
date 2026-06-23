const CryptoJS = require('crypto-js');
const axios = require('axios');

async function test() {
    const rawPayload = { email: 'ashish.srivastava1@jindalstainless.com', password: 'Welcome@123' };
    const jsonString = JSON.stringify(rawPayload);

    const key = CryptoJS.enc.Utf8.parse('JslLibrarySecretKeyForLogin2026!');
    const iv = CryptoJS.enc.Utf8.parse('JslLibraryLogIv1');

    const encrypted = CryptoJS.AES.encrypt(jsonString, key, {
      iv: iv,
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7
    }).toString();

    try {
        const res = await axios.post('https://localhost:7035/api/auth/login', 
            { encryptedData: encrypted },
            { httpsAgent: new require('https').Agent({ rejectUnauthorized: false }) }
        );
        console.log('Status:', res.status);
        console.log('Response:', JSON.stringify(res.data, null, 2));
    } catch (e) {
        if (e.response) {
            console.log('Error Status:', e.response.status);
            console.log('Error Data:', JSON.stringify(e.response.data, null, 2));
        } else {
            console.error(e.message);
        }
    }
}
test();
