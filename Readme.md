# IP Watcher

![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/jonsofte/ip-watcher/release.yml)
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/jonsofte/ip-watcher)

A simple service for monitoring an unexpected change of the public IP address of a host/cluster.

If a service is running on a host with a public facing IP address that might change unexpectedly, this service will detect the change, and update an external store (Azure Blob Storage) with the new IP address. 

If an external client which is dependent on the internal service, detects that the service is no longer available, it can get the new IP address from the store, update its internal configuration, and reconnect to the service that is now being exposed on the new IP address.

## Description

* For detecting the current IP address, the service calls the [ipify.org](https://www.ipify.org/) public REST API.
* The previous registered IP address is stored in a [Azure Storage Container Blob](https://azure.microsoft.com/en-us/products/storage/blobs). 
* The application authenticates to Azure with an Application Service Principal. The principal has an assigned role that has read/write access to the specified storage container.
* A cron job triggers and runs the service at a fixed interval.
* The current public IP address is compared to the previous registered IP address. If it has changed, the new IP address is persisted to the Blob storage container.
* The service is implemented with .NET 7.
* A Helm chart is provided for easy installation and configuration on a Kubernetes cluster.

![Architecture](https://user-images.githubusercontent.com/24587666/225579998-6ebf0bd8-d5f9-46d1-9e34-96bf5cfe007a.png)

## Logging and Tracing

* Logs are written to the Console (stdout). 
* Tracing is optional, and may be sent to an [Open Telemetry Collector](https://opentelemetry.io/docs/collector/). Forwarding of traces is done by enabeling the feature flag and specifying an URI to an OTLP HttpProtobuf protocol enabled endpoint. See the Configuration Parameters

## Installation and configuration of Azure Resources and Certificate

1. Create a Storage Account in Azure ([Docs](https://learn.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-cli)). Take note of the Account URI.
2. Create a Blob Container in the Storage Account ([Docs](https://learn.microsoft.com/en-us/azure/storage/blobs/blob-containers-cli#create-a-container)). Take not of the Container name.
3. [Create the Application Service Principal in Azure AD](#create-the-application-service-principal-in-azure-ad).
4. [Generate a x509 certificate](#generate-a-x509-certificate) (To be used for Authentication between the app and the Azure Blob files).
5. [Assign the certificate to the Application Service Principal](#upload-the-certificate-to-azure-ad-and-bind-it-to-the-application-id).
6. [Create a Role Binding on the Blob Storage Container that gives the SP read/write access to the Blob](#assign-readwrite-access-to-application-service-principal).
7. [Generate a PFX Certificate from the x509 certificate](#generate-a-pfx-certificate-to-be-used-in-the-service) (To be used in the container for accessing the Blob).
6. Add the ip-watcher as a helm repository
7. Configure the application and install with helm

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

The Blob Storage is now ready to accept requests via the Application Service Principal that is identified with the certificate.

### Generate a PFX Certificate to be used in the IP Watcher application

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
```

The created certificate file will be used during the helm configuration and installation.

## On the local cluster: Install and Configure IP Watcher with helm

### Add IP Watcher as a Helm Repo

Add ip-watcher to your helm repo: 
``` bash
$ helm repo add ip-watcher 'https://raw.githubusercontent.com/jonsofte/ip-watcher/master/helm/charts'
```

Update and list its contents to verify that the repo and chart is found:
``` text
$ helm repo update ip-watcher
...
$ helm search repo ip-watcher

NAME                    CHART VERSION   APP VERSION     DESCRIPTION
ip-watcher/ip-watcher   1.0.0           0.9.2           Service for monitoring unexpected change of pub...
```

### Configure and install the service

Create a local instance of the `values.yaml` configuration file:

``` bash
$ helm show values ip-watcher/ip-watcher > values.yaml
```

Edit the file and configure the values as described in the helm parameter configuration section. Alternatively use the `--set`  parameter with the `helm install` command. The `certificate` parameter must be a base64 encoded version of the PFX Certificate. 

Install the helm chart:

``` bash
$ helm install -f values.yaml --set certificate="$(base64 -w0 ip-watcher.pfx)" \
    --generate-name ip-watcher/ip-watcher
```

## Helm Configuration Parameters

The following table lists the configurable parameters of the IP Watcher and their default values.

| Parameter                           | Description                                                   | Default                                                  |
|-------------------------------------|---------------------------------------------------------------|----------------------------------------------------------|
| `loglevel`                          | Application Log Level                                         | `Information`                                            |
| `cronSchedule`                      | Cron Schedule for triggering a check. See [crontab guru](https://crontab.guru/)  | `0 0 * * *` (At Midnight)  |
| `certificate`                       | Base64 encoded content of the PFX certificate | `nil` **(Must be provided)** |
| `azure.blob.accountUri`             | URL to the Azure Storage Blob. Example: `https://****.blob.core.windows.net/` | `nil` **(Must be provided)**|
| `azure.blob.containerName`          | Blob Container name | `nil` **(Must be provided)**|
| `azure.blob.currentIpFile`          | Filename for JSON document in container with the current registered IP address | `ip_watcher_current_ip.json` |
| `azure.blob.logFile`                | Filename for JSON document in container that contains a log of the previous registered IP addresses | `ip_watcher_change_log.json` |
| `azure.auth.certPassword` | Password for the PFX certificate | `nil` **(Must be provided)** |
| `azure.auth.tentantID` | Azure AD Tenant ID for authenticating the application with Azure AD. In GUID format: `00000000-0000-0000-0000-000000000000`  | `nil` **(Must be provided)**|
| `azure.auth.clientID` | Azure AD Client ID for authenticating the application with Azure AD. In GUID format: `00000000-0000-0000-0000-000000000000` | `nil` **(Must be provided)**|
| `otel.enable` | True/False: Enable OTEL exporter for sending Tracing to an OpenTelemetry Collector  | `false` |
| `otel.endpoint` | URI to OpenTelemetry Traces Collector. The exporter is using the HttpProtobuf protocol. Example of URI: `http://<service>:4318/v1/traces`  | `nil` (Must be provided) |

