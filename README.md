# kiss-proxy

Keep it simple stupid - proxy

## Goal

Listen to http and tcp requests -> forward the requests directly or through others proxies depending on rules you define

## Use cases

### Rerouting http requests

Set your browser or any application to send their http requests to this proxy. You are then able to redirect the requests to a third party proxy only if the requested url contains www.google.com

### Foward tcp requests

Requests headed to port 666 on your machine are redirected to port 80 on google.com

### Notes about the certificat

When using this proxy for HTTPS connexions, you need to add the generated certificate (rootCert.pfx) as a trusted party on your endpoint machine (user machine).
Otherwise you will have certificate issues since kissproxy is in fact a "Man in the middle".
Use certmgr.msc, add the certificate to your personal as well as the trusted root store.