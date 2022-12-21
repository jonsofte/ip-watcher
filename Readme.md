# IP Watcher Tool

A simple tool for tracking the current public IP address of a host. 

At a regular interval the current public IP address is compared with the previous registered address. If it is changed, the new address is saved to an Azure Container Blob. 

This is for helping with the challenge of Internet service providers changing the IP address when the internet connection is restarted, and it thus allows for external clients to reconnect to services that is exposed on that IP address.
