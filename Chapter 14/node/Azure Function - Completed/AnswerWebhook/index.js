const { connect } = require('http2');

module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');
    const crypto = require('crypto');
    const sharedSecret = "jKCFTZeeyZoqV6tupFLbu77gKTmAUlYd80FvBm8YQN8="; // e.g. "+ZaRRMC8+mpnfGaGsBOmkIFt98bttL5YQRq3p2tXgcE="
    const bufSecret = Buffer(sharedSecret, "base64");

    var auth = req.headers['authorization'];
    var msgBuf = Buffer.from(context.req.rawBody, 'utf8');
    var msgHash = "HMAC " + crypto.createHmac('sha256', bufSecret).update(msgBuf).digest("base64");

    var receivedMsg = req.body.text;
    var responseMessage = '{ "type": "message", "text": "You typed: ' + receivedMsg + '" }';

    if (msgHash === auth) {
         //authentication ok
    } else {
        responseMsg = '{ "type": "message", "text": "Error: message sender cannot be authenticated." }';
    }
 

    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };
}