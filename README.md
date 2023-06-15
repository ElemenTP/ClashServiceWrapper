# Clash Service Wrapper

#### clash as a service for windows

---

This simple tool enables clash to run as a Windows service on the background, while looks like running directly. This tool aims at using TUN mode without a UAC dialog and avoiding gui app get privileged which may cause security issues.

This tool (ClashServiceWrapper.exe) install itself as an Windows service. After installation, when it is launched, it starts the service which is another instance of this tool. The second instance starts and monitors a clash process when running on the background. The first instance receives logs from clash (logs are from stdout & stderr of clash process) through named pipes connection with the second instance. You can also just fire the service without receiving logs through command line options.

This tool is powered by .NET 7 and NativeAOT, theoretically it can be used on Windows 7 or newer without any runtime. But I only tested it on my computer running Windows 11.

---

#### Usage

1. Rename your clash executable to clash.exe. Place ClashServiceWrapper.exe and clash.exe together somewhere non-Administrators can not modify for safty.
2. Use a privileged shell (cmd, powershell, etc.), run ClashSvcHost.exe with argument "install", if output is `INFO: Service 'Clash Hosting Service' installed successfully.` then the service is successfully installed and can continue.*
3. Start ClashServiceWrapper.exe by double click or a non-privileged shell with arguments. All arguments will be sent to the service, clash process will start with those arguments.
4. To stop the service and clash process, send ctrl-c to the console. This will stop the service and clash process most gracefully. If the client process is killed, the host service will detect and stop gracefully. If you stop the service manually by taskmgr or services.msc, the service will stop gracefully and the client will detect this and stop gracefully.
5. To uninstall the service, use a privileged shell (cmd, powershell, etc.), run ClashServiceWrapper.exe with argument "uninstall", if output is `INFO: Service 'Clash Hosting Service' was uninstalled successfully.` then the service is successfully uninstalled.
6. You can also use argument "start" to fire the service without receiving logs. After that, you can use "status" to query service status and "stop" to stop the service.

 *The service is set access rules (DACL) to allow non-privileged users to start, stop and query status.

---

Thanks:
[winsw/WinSW](https://github.com/winsw/WinSW) some code of this project is from winsw.
