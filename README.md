HTTPS Beacon Shell
==================

Based on [HARS server from @OnSec-fr](https://github.com/onSec-fr/Http-Asynchronous-Reverse-Shell).

Many new features added and changed.

Main Features
-------------
1. HTTP compliant. Uses GET/POST request, hence would fit into normal traffic.
2. GZIP-like encoding. Additional obfuscation on top of SSL. Encoded with GZIP and reversed the byte array.
3. In-band File Transfer. Tested up to 200MB file size.


Commands
-------------
```
For configuration parameters, check out Config.cs for client and HBS_Server.py for server.


STARTSH                 - Start shell process. We don't want cmd.exe running in the background all the time.
EXITSH                  - Exit shell process.
EXITPROC                - Exit entire reverse shell.
SLEEP XXX               - SLEEP 60 for 60 minutes sleep. We don't want constant callback all the time.
FILED /path/here.bin    - Download /path/here.bin to current directory as file.dat
FILEU C:\file.exe       - Upload C:\file.exe to current directory as file.dat
ACTIVE                  - Active mode, shorter beaconing delay
INACTIVE                - Inactive mode, longer beaconing delay.
```

Screenshot
--------------
![image](https://raw.githubusercontent.com/limbenjamin/HTTPSBeaconShell/master/Images/screenshot.png)
