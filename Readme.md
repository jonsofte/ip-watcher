# IP Watcher Tool

A simple tool for tracking the current public IP address of a host.

If a service is running on a host or a cluster that is behind a poublic ip address that might change unexpectedly, this service will notice the change, and update a public accesible store with the new ip address. When external clients that are dependent on the service notice that the service is not accessible, it can then poll the ip address from the store, update it's internal configuration, and then rediscover and reconnect to the service that is now beeing exposed on the new ip address. 

## Impementation

The application is implemented in .Net. For detecting the current ip address, a client calls the ipify.net public http service. The previous registered ip address is stored in a Azure Storage Container Blob. The application authenticates to Azure with a service principal with an assigned role that has read/write access to the specified storage container. 

A cron job in the application is running at a specified interval. The current public IP address is then compared to the previous registered address. If it has changed, the new address is persisted to the storage container.

Open Telemetry is beeing used to monitor the application. Logs and Traces are beeing collected. Depending on the setup of the Open Telemetry receiver, traces might be monitored in Jaeager, and Logs might be viewed in ElastiSearch.

## Configuration

### Configuration of Service Principal in Azure AD


``` sh
# Create an Azure AD Application service principal for the application
$ az ad app create --display-name ip-watcher-service
{ ... }

# Get the Application ID from the newly created service principal
$ az ad app list | jq '.[] | select(.displayName=="ip-watcher-service").appId'

"00000000-0000-0000-0000-000000000000"

# With OpenSSL, generate a x509 certificate with a corresponding private key
# -days (x)     # Specify number of days before certificate expiration
# -keyout (x)   # Output name for the generated Base64 encoded private key
# -out (x)      # Output name for the generated Base64 encoded certificate
# -subj (x)     # Subject string for certificate. Edit as needed, or remove for manual input
# Note: Azure AD will not accept the certificate if Issuer subject is not set correctly
$ openssl req -newkey rsa:2048 -new -nodes -x509 -days 365 -keyout ip-watcher-key.pem -out ip-watcher-cert.pem -subj "/C=/O=/OU=/CN="

Generating a RSA private key
..............................................+++++
...

# Upload the certificate to Azure AD and assign it to the application with the Application ID
# --id (x)      # The Application ID in Azure. From the second step
# --cert (x)    # Path to the Base64 encoded certificate
# Take note of the Tenant ID returned from Azure

$ az ad app credential reset --id <application-id> --cert '@./ip-watcher-cert.pem' --append

{ ... }

# The certificate will be uploaded 

# In the Azure Storage Container, assign the *Role* Role to the application ID, so that it will have the rights to read and write to the container.
# *******

# The Azure AD Application is now ready to accept requests

# Genereate a pfx file to be used in the application  for authentication against the newly created certificate. Enter a password when requested
# -out (x)      # Output name for the genereated pfx file
# -inkey (x)    # Input name for the private key
# -in (x)       # Input name for the x509 certificate

$ openssl pkcs12 -export -out ip-watcher.pfx -inkey ip-watcher-key.pem -in ip-watcher-cert.pem

{ ... }
```

### Configuration of Container

## Installation with Helm


