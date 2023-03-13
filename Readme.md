# IP Watcher

![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/jonsofte/ip-watcher/release.yml)
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/jonsofte/ip-watcher)

A simple tool for monitoring the current public IP address of a host/cluster.

If a service is running on a host with a public facing IP address that might change unexpectedly, this service will detect the change, and update an external store (Azure Blob Storage) with the new IP address. 

An external client which is dependent on the internal service, that detects that the service is no longer available, can then get the new IP address from the store, update its internal configuration, and reconnect to the service that is now being exposed on the new IP address.

## Impementation

* For detecting the current IP address, the service calls the [ipify.org](https://www.ipify.org/) public REST API.
* The previous registered IP address is stored in a [Azure Storage Container Blob](https://azure.microsoft.com/en-us/products/storage/blobs). 
* The application authenticates to Azure with an Application Service Principal. The principal has an assigned role that has read/write access to the specified storage container.
* A cron job triggers and runs the service at a regular interval.
* The current public IP address is compared to the previous registered address. If it has changed, the new address is persisted to the storage container.
* Open Telemetry is provided from the service. Logs and Traces are being forwarded to an [Open Telemetry Collector](https://opentelemetry.io/docs/collector/). This enables Traces to be monitored in Jaeger, and Logs can be viewed in ElasticSearch.
* The service is imlpemented with .NET 7.

## Installation and configuration

1. Create a Storage Account in Azure ([Docs](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-cli)). Take note of the Account URI.
2. Create a Blob Container in the Storage Account ([Docs](https://learn.microsoft.com/en-us/azure/storage/blobs/blob-containers-cli#create-a-container)). Take not of the Container name.
3. [Create the Application Service Principal in Azure AD](#create-the-application-service-principal-in-azure-ad).
4. [Generate a x509 certificate](#generate-a-x509-certificate) (To be used for Authentication between the app and the Azure Blob files).
5. [Assign the certificate to the Application Service Principal](#upload-the-certificate-to-azure-ad-and-bind-it-to-the-application-id).
6. [Create a Role Binding on the Blob Storage Container that gives the SP read/write access to the Blob](#assign-readwrite-access-to-application-service-principal).
7. [Generate a PFX Certificate from the x509 certificate](#generate-a-pfx-certificate-to-be-used-in-the-service) (To be used in the container for accessing the Blob).
6. Use Helm to install the application on a local cluster, or install the container manually.
7. Configure the application via the Helm values.yaml file.

---

### Create the Application Service principal in Azure AD

``` sh
# Create an Azure AD Application service principal for the application
$ az ad app create --display-name ip-watcher-service
{ ... }

# Get the Application ID from the newly created Service Principal
$ az ad app list | jq '.[] | select(.displayName=="ip-watcher-service").appId'

"00000000-0000-0000-0000-000000000000"

```

### Generate a x509 Certificate

``` sh
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
```

### Upload the certificate to Azure AD and bind it to the Application ID

``` sh
# Upload the certificate to Azure AD and bind it to the Application ID
# --id (x)      # The Application ID in Azure. From the second step
# --cert (x)    # Path to the Base64 encoded certificate
# Take note of the Tenant ID returned from Azure

$ az ad app credential reset --id <application-id> --cert '@./ip-watcher-cert.pem' --append

{ ... }
```

### Assign read/write access to Application Service Principal

``` sh
# Set the Scope for the role binding to reference the correct blob container.
# Replace subscription, resource-group, storage-account, and container
scope="/subscriptions/<subscription>/resourceGroups/<resource-group>/providers/Microsoft.Storage/
storageAccounts/<storage-account>/blobServices/default/containers/<container>"

# Assign the "Storage Blob Data Contributor" role to the application with the created scope
az role assignment create --role "Storage Blob Data Contributor" \
    --assignee <application-id> --scope $scope
```

The Application Service Principal is now ready to accept requests

### Generate a PFX Certificate to be used in the service

``` sh
# When requested to enter and create a new password, take note of the password that is created. 
# The certificate must later be mounted to the container and referenced as an environment 
# variable. 
# -out (x)      # Output name for the genereated pfx file
# -inkey (x)    # Input name for the private key
# -in (x)       # Input name for the x509 certificate

$ openssl pkcs12 -export -out ip-watcher.pfx -inkey ip-watcher-key.pem \
-in ip-watcher-cert.pem

{ ... }
###
```

### Helm Parameters

The following table lists the configurable parameters of the IP Watcher and their default values.

| Parameter                           | Description                                                   | Default                                                  |
|-------------------------------------|---------------------------------------------------------------|----------------------------------------------------------|
| `loglevel`                          | Application Log Level                                         | `Information`                                            |
| `cronSchedule`                      | Cron Schedule for triggering a run of the check. See [crontab guru](https://crontab.guru/)  | `* 0 * * *`                |
| `azure.blob.accountUri`             | URL to the Azure Storage Blob. Example: `https://****.blob.core.windows.net/` | `nil` **(Must be provided)**|
| `azure.blob.containerName`          | Blob Container name | `nil` **(Must be provided)**|
| `azure.blob.currentIpFile`          | Filename for JSON document in container with the current registered IP Address | `ip_watcher_current_ip.json` |
| `azure.blob.logFile`                | Filename for JSON document in container that contains a log of previous registered IP Addresses | `ip_watcher_change_log.json` |
| `azure.auth.certificatePath` | Path of the PFX Certificate that is being used in the application to authenticate to the Azure Application Service Principal. Note: If value is changed, the file must also be mounted into the container at the same path. | `/certificates/certwithkey.pfx` |
| `azure.auth.certificatePassword` | Password for the PFX certificate | `nil` **(Must be provided)** |
| `azure.auth.tentantID` | Azure AD Tenant ID for authenticating the application with Azure AD. In GUID format: `00000000-0000-0000-0000-000000000000`  | `nil` **(Must be provided)**|
| `azure.auth.clientID` | Azure AD Client ID for authenticating the application with Azure AD. In GUID format: `00000000-0000-0000-0000-000000000000` | `nil` **(Must be provided)**|

