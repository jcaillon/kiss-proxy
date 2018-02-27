# kiss-proxy

Keep it simple stupid - proxy

## Goal

Listen to http and tcp requests -> forward the requests directly or through others proxies depending on rules you define

## Use cases

### Rerouting http requests

Set your browser or any application to send their http requests to this proxy. You are then able to redirect the requests to a third party proxy only if the requested url contains www.google.com

### Foward tcp requests

Requests headed to port 666 on your machine are redirected to port 80 on google.com

## HTTPS and certificates

When using this proxy for HTTPS connexions, you need to add the generated root certificate (rootCert.pfx) as a trusted certification authority on the machine that will use this proxy.
Otherwise you will have certificate issues since kissproxy is in fact a "Man in the middle".

### Windows certificate store

On windows, use `certmgr.msc`, to add the certificate to your **trusted root certification authorities** store.

### JAVA certificate store

If you plan to use this proxy for JAVA applications as well, you have to know that JAVA uses its own certificate store.

First, convert the .pfx certificate to a .cer format using [openssh](https://www.openssh.com/) (you can also export it through certmgr.msc) :

```
openssl pkcs12 -in rootCert.pfx -out rootCert.crt -nokeys -clcerts
openssl x509 -inform pem -in rootCert.crt -outform der -out rootCert.cer
```

Secondly, import this .cer file with `keytool` to the java keystore which can be found under `JAVA_HOME/lib/security/cacerts` or `JAVA_HOME/jre/lib/security/cacerts` if you are using a JDK :

```
keytool -import -trustcacerts -noprompt -keystore <full path to cacerts> -storepass changeit -alias $REMHOST -file $REMHOST.cer
```

Example :

```
"C:\Program Files\Java\jdk1.8.0_162\bin\keytool.exe" -import -trustcacerts -noprompt -keystore "C:\Program Files\Java\jdk1.8.0_162\jre\lib\security\cacerts" -storepass changeit -alias "titaniumrootcertificate" -file "rootCert.cer"
```
