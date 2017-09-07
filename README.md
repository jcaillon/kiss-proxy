# kiss-proxy

Keep it simple stupid - proxy

## Goal

Listen to http and tcp requests -> forward the requests directly or through others proxies depending on rules you define

## Use cases

### Rerouting http requests

Set your browser or any application to send their http requests to this proxy. You are then able to redirect the requests to a third party proxy only if the requested url contains www.google.com

### Foward tcp requests

Requests headed to port 666 on your machine are redirected to port 80 on google.com