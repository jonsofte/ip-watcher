# IP Watcher

![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/jonsofte/ip-watcher/release.yml)
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/jonsofte/ip-watcher)

A simple tool for monitoring the current public IP address of a host/cluster.

If a service is running on a host with a public facing IP address that might change unexpectedly, this service will notice the change, and update an external store with the new IP address. 

An external client which is dependent on the internal service, that detects that the service is no longer available, can then get the IP address from the store, update its internal configuration, and reconnect to the service that is now being exposed on the new IP address.

## Impementation

* The application is implemented in .Net. 
* For detecting the current IP address, a client calls the [ipify.org](https://www.ipify.org/) public REST API.
* The previous registered IP address is stored in a [Azure Storage Container Blob](https://azure.microsoft.com/en-us/products/storage/blobs). 
* The application authenticates to Azure with an Application Service Principal. The principal has an assigned role that has read/write access to the specified storage container.
* A cron job triggers the application at a regular interval.
* The current public IP address is then compared to the previous registered address. If it has changed, the new address is persisted to the storage container.
* Open Telemetry is being provided to monitor the application. Logs and Traces are being forwarded to an [Open Telemetry Collector](https://opentelemetry.io/docs/collector/). Traces can then be monitored in Jaeger, and Logs can be viewed in ElasticSearch.

## Configuration

1. Create the Blob Storage Resource in Azure
2. Create the Application Service principal in Azure AD
3. Assign a Certificate to the Service Principal
4. Create a Role Binding on the Blob Storage Container that gives the SP read/write access
5. Install the IP-watcher tool
6. Configure the application

### Creation and Configuration of Service Principal in Azure AD (Points 2 to 4) with the az command tool

``` sh
# Create an Azure AD Application service principal for the application
$ az ad app create --display-name ip-watcher-service
{ ... }

# Get the Application ID from the newly created Service Principal
$ az ad app list | jq '.[] | select(.displayName=="ip-watcher-service").appId'

"00000000-0000-0000-0000-000000000000"

# With OpenSSL, generate a x509 certificate with a corresponding private key
# -days (x)     # Specify number of days before certificate expiration
# -keyout (x)   # Output name for the generated Base64 encoded private key
# -out (x)      # Output name for the generated Base64 encoded certificate
# -subj (x)     # Subject string for certificate. Remove for manual input
# Note: Azure AD will not accept the certificate if Issuer subject is not set correctly
$ openssl req -newkey rsa:2048 -new -nodes -x509 -days 365 -keyout ip-watcher-key.pem \
    -out ip-watcher-cert.pem -subj "IP-Watcher Service"

Generating a RSA private key
..............................................+++++
...

# Upload the certificate to Azure AD and bind it to the Application ID
# --id (x)      # The Application ID in Azure. From the second step
# --cert (x)    # Path to the Base64 encoded certificate
# Take note of the Tenant ID returned from Azure

$ az ad app credential reset --id <application-id> --cert '@./ip-watcher-cert.pem' --append

{ ... }

# Assign read/write access to Application Service Principal

# Set the scope for the role to reference the blob container.
# Add subscription, resource-group, storage-account, and container
scope="/subscriptions/<subscription>/resourceGroups/<resource-group>/providers/Microsoft.Storage/
storageAccounts/<storage-account>/blobServices/default/containers/<container>"

# Assign the Storage Blob Data Contributor role with the correct scope to the Application
az role assignment create --role "Storage Blob Data Contributor" \
    --assignee <application-id> --scope $scope

# The Application Service Principal is now ready to accept requests

# Generate a pfx file to be used in the application for authentication to the newly
# created Service Princial. Enter a password when requested. The certificate must be
# mounted to the container and referenced as and environment variable. The password
# should be stored as a secret.
# -out (x)      # Output name for the genereated pfx file
# -inkey (x)    # Input name for the private key
# -in (x)       # Input name for the x509 certificate

$ openssl pkcs12 -export -out ip-watcher.pfx -inkey ip-watcher-key.pem \
-in ip-watcher-cert.pem

{ ... }
###

```

### Configuration of the container/service

The following Environment Variables must be provided to the container at startup:

``` sh
IPWatcher_Serilog__MinimumLevel__Default
IPWatcher_CronScheduleConfiguration__CronSchedule
IPWatcher_AzureStorageConfiguration__Authentication__X509CertificatePath
IPWatcher_AzureStorageConfiguration__Blob__AccountUri
IPWatcher_AzureStorageConfiguration__Blob__ContainerName
IPWatcher_AzureStorageConfiguration__Blob__CurrentIPFile
IPWatcher_AzureStorageConfiguration__Blob__ChangeLogFile
IPWatcher_AzureStorageConfiguration__Authentication__X509Password
IPWatcher_AzureStorageConfiguration__Authentication__AzureADTenantID
IPWatcher_AzureStorageConfiguration__Authentication__AzureADClientID
```

**IPWatcher_Serilog__MinimumLevel__Default:**  
The log level of the application.  
Example: **Information** or **Debug**

**IPWatcher_CronScheduleConfiguration__CronSchedule:**  
The Cron schedule for triggering a run of the check. Se https://crontab.guru/  
Example: * 10 * * *

**IPWatcher_AzureStorageConfiguration__Authentication__X509CertificatePath:**  
Path of the PFX Certificate that is being used in the application to authenticate to the Azure Application SP. Note: The file must also be mounted into the container at the same path.  
Example: /certificates/certwithkey.pfx

**IPWatcher_AzureStorageConfiguration__Blob__AccountUri:**  
URL to the Azure Storage Blob.  
Example: https://****.blob.core.windows.net/

**IPWatcher_AzureStorageConfiguration__Blob__ContainerName:**  
Blob Container name.  
Example: ip-watcher

**IPWatcher_AzureStorageConfiguration__Blob__CurrentIPFile:**  
Filename for JSON document in container with the current registered IP Address.  
Example: ip_watcher_current_ip.json

**IPWatcher_AzureStorageConfiguration__Blob__ChangeLogFile:**  
Filename for JSON document in container that contains a log of previous registered IP Addresses.  
Example: ip_watcher_change_log.json

**The following variables should be added to the container as secrets:**  

**IPWatcher_AzureStorageConfiguration__Authentication__X509Password:**  
Password to be used when using the PFX certificate for authentication.  

**IPWatcher_AzureStorageConfiguration__Authentication__AzureADTenantID:**  
Azure AD Tenant ID for authenticating the application with Azure AD.  
Example GUID format: 00000000-0000-0000-0000-000000000000

**IPWatcher_AzureStorageConfiguration__Authentication__AzureADClientID:**  
Azure AD Client ID for authenticating the application with Azure AD  
Example GUID format: 00000000-0000-0000-0000-000000000000

