## Create a self-signed certificate and trust it on Windows

You can use already created certificate from `./win/localhost.cer` folder. 

But also you can create one and add it to trusted certificates by the script below:
1. Open Powershell with administrator privileges
2. Before run the script you should set the following environment variable:
```powershell
$Env:LocalhostPass = "<password-value>"
```
3. Run the script: `.\win\selfsigned_cert_and_trust_windows.ps1`

## Create a self-signed certificate and trust it on Mac OSX

Ensure the OpenSSL library is installed on Max OSX.
Run the following command to check if OpenSSL is installed

```shell
which openssl
```

If OpenSSL is not installed, install OpenSSL with `brew`

```shell
brew install openssl
```

Run the following 2 commands using OpenSSL to create a self-signed certificate in Mac OSX with OpenSSL from `./osx/` folder:

```shell
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout localhost.key -out localhost.crt -config localhost.conf -passin pass:testpass

sudo openssl pkcs12 -export -out localhost.pfx -inkey localhost.key -in localhost.crt
```

### Add to Trust Certificates (Mac OSX)

In `Keychain Access`, double-click on this new localhost cert. Expand the arrow next to "Trust" and choose to "Always trust". Chrome and Safari should now trust this cert.

or from terminal:

```
sudo security add-trusted-cert -p ssl -d -r trustRoot -k ~/Library/Keychains/login.keychain localhost.crt
```

If you will not add certificate to trusted ones, in that case Chrome will tell you that the certificate is invalid (since it’s self-signed), and will ask you to confirm before continuing (**_however, the HTTPS connection will still work_**).

## How to use certificate with NodeJS

```js
const https = require('https')
const app = express()

app.get('/', (req, res) => {
  res.send('Hello HTTPS!')
})

https.createServer({
  key: fs.readFileSync('server.key'),
  cert: fs.readFileSync('server.cert')
}, app).listen(3000, () => {
  console.log('Listening...')
})
```




