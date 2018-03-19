# kiss-proxy

Keep it simple stupid - proxy

## Goal

Listen to http and tcp requests -> forward the requests directly or through others proxies depending on rules you define

## Use cases

### Rerouting http requests

Set your browser or any application to send their http requests to this proxy. You are then able to redirect the requests to a third party proxy only if the requested url contains www.google.com

### Foward tcp requests

Requests headed to port 666 on your machine are redirected to port 80 on google.com

## config.xml example

```xml
<?xml version="1.0"?>
<Config xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Proxies>
    <Proxy>
      <LocalAddress>127.0.0.1</LocalAddress>
      <LocalPort>666</LocalPort>
      <ExternalProxyRules>
        <ExternalProxyRule>
          <RegexUrlMatch>.*(localhost|127\.0\.0\.1).*</RegexUrlMatch>
          <ProxyHost>NoProxy</ProxyHost>
          <ProxyPort>0</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
        <ExternalProxyRule>
          <RegexUrlMatch>.*toredirect.*</RegexUrlMatch>
          <ProxyHost>192.168.213.137</ProxyHost>
          <ProxyPort>3128</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
        <ExternalProxyRule>
          <RegexUrlMatch>.*otherredirect\.com.*</RegexUrlMatch>
          <ProxyHost>172.27.25.3</ProxyHost>
          <ProxyPort>8080</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
        <ExternalProxyRule>
          <RegexUrlMatch>.*</RegexUrlMatch>
          <ProxyHost>SystemWebProxy</ProxyHost>
          <ProxyPort>0</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
      </ExternalProxyRules>
    </Proxy>
    <Proxy>
      <LocalAddress />
      <LocalPort>666</LocalPort>
      <ExternalProxyRules>
        <ExternalProxyRule>
          <RegexUrlMatch>.*test.*</RegexUrlMatch>
          <ProxyHost>192.168.213.137</ProxyHost>
          <ProxyPort>3128</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
        <ExternalProxyRule>
          <RegexUrlMatch>.*</RegexUrlMatch>
          <ProxyHost>172.27.25.3</ProxyHost>
          <ProxyPort>8080</ProxyPort>
          <ProxyUsername />
          <ProxyPassword />
        </ExternalProxyRule>
      </ExternalProxyRules>
    </Proxy>
  </Proxies>
  <TcpForwarders>
    <TcpForwarder>
      <LocalAddress />
      <LocalPort>667</LocalPort>
      <DistantAddress>172.27.50.55</DistantAddress>
      <DistantPort>80</DistantPort>
    </TcpForwarder>
  </TcpForwarders>
  <LogRules>
    <LogRule>
      <RegexUrlMatch>.*</RegexUrlMatch>
      <ClientIp></ClientIp>
    </LogRule>
  </LogRules>
</Config>
```

### HTTPS and certificates

When using this proxy for HTTPS connexions, you need to add the generated certificate (rootCert.pfx) as a trusted party on the machine that will use this proxy.
Otherwise you will have certificate issues since kissproxy is in fact a "Man in the middle".

### Windows certificate store

On windows, use `certmgr.msc`, to add the certificate to your **trusted root certification authorities** store.

### JAVA certificate store

If you plan to use this proxy for JAVA applications as well, you have to know that JAVA uses its own certificate store.

First, convert the .pfx certificate to a .cer format using [openssh](https://www.openssh.com/) (you can also export it through certmgr.msc) :

```
openssl pkcs12 -in rootCert.pfx -out rootCert.crt -nokeys -clcerts
openssl x509 -inform pem -in rootCert.crt -outform der -out rootCert.cer
openssl x509 -in rootCert.crt -outform PEM -out rootCert.pem
```

Secondly, import this .cer file with `keytool` to the java keystore which can be found under `JAVA_HOME/lib/security/cacerts` or `JAVA_HOME/jre/lib/security/cacerts` if you are using a JDK :

```
keytool -import -trustcacerts -noprompt -keystore <full path to cacerts> -storepass changeit -alias $REMHOST -file $REMHOST.cer
```

Example :

```
"C:\Program Files\Java\jdk1.8.0_162\bin\keytool.exe" -import -trustcacerts -noprompt -keystore "C:\Program Files\Java\jdk1.8.0_162\jre\lib\security\cacerts" -storepass changeit -alias "titaniumrootcertificate" -file "rootCert.cer"
```

### Git

Follow this guide : [adding a self signed certificate authority to git store](https://blogs.msdn.microsoft.com/phkelley/2014/01/20/adding-a-corporate-or-self-signed-certificate-authority-to-git-exes-store/)

Switch to using a private copy of the Git root certificate store

```bash
copy "C:\Program Files\git\mingw32\ssl\certs\ca-bundle.crt" C:\Users\yourname
```
```git
git config --global http.sslCAInfo C:\Users\yourname\ca-bundle.crt
```

Then add the exported root certificate to the private copy of the store (edit ca-bundle.crt and add the content of your certificate in this text file)

