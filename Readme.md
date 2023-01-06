# IP Watcher Tool

A simple tool for tracking the current public IP address of a host. 

At a regular interval the current public IP address is compared with the previous registered address. If it has changed, the new address is saved to an Azure Container Blob. 

This helps with the issue of Internet Service Providers changing the IP address when the internet connection is restarted, and thus allows for external clients to rediscover and reconnect to the services that are exposed on the host.
