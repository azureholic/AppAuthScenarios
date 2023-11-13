# this PowerShell script generates a cert and exports it to a file
# 1. upload the file to Entra ID as a certificate for the ServicePrincipal
# 2. the "Program.cs" file reads the certificate from the currentUser cert store 
#    on Windows and uses it to authenticate


$cert = New-SelfSignedCertificate -CertStoreLocation "cert:\CurrentUser\My" `
  -Subject "CN=selfsigned-demo" `
  -KeySpec KeyExchange

Export-Certificate -Cert $cert -FilePath .\servicePrincipal.cer