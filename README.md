# Clash Service Wrapper
#### clash as a service for windows
***
This simple tool enables clash to run as a Windows service on the background, while looks like running directly. This tool aims at using TUN mode without a UAC dialog and avoiding gui app get privileged which may cause security issues.  
  
This tool contains two apps: host(ClashSvcHost.exe) and client(ClashSvcClient.exe). The host app starts and monitors a clash process when running as a Windows service on the background. The client app starts the service, and receives logs from clash (logs are from stdout & stderr of clash process) through named pipes.  
  
This tool is powered by .NET 6 and NativeAOT, theoretically it can be used on Windows 7 or newer without any runtime. But I only tested it on my computer running Windows 11.
***
Usage:  
1. Rename your clash executable to clash.exe. Place ClashSvcHost.exe and clash.exe together somewhere non-Administrators can not modify for safty.  
2. Use a privileged shell (cmd, powershell, etc.), run ClashSvcHost.exe with argument "install", if output is `INFO: Service 'Clash Hosting Service' installed successfully.` then the service is successfully installed and can continue.*
3. Start ClashSvcClient.exe by double click or a non-privileged shell with arguments. The client will send all arguments to the host and the host will start clash process with those arguments.  
4. To stop the service and clash process, send ctrl-c to the client process. This will stop the service and clash process most gracefully. If the client process is killed, the host service will detect and stop gracefully. If you stop the service manually by taskmgr or services.msc, the service will stop gracefully and the client will detect this and stop gracefully.  
5. To uninstall the service, use a privileged shell (cmd, powershell, etc.), run ClashSvcHost.exe with argument "uninstall", if output is `INFO: Service 'Clash Hosting Service' was uninstalled successfully.` then the service is successfully uninstalled.  
  
 *The service is set access rules (DACL) to allow non-privileged users to start, stop and query status.  
***
Thanks:  
[winsw/WinSW](https://github.com/winsw/WinSW) some code of this project is from winsw.  
