# SharpCookieMonster
```
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
                                                                                
                                                                              
                         SharpCookieMonster v1.0 by @m0rv4i

```

 ## Description
 
 This is a Sharp port of @defaultnamehere's [cookie-crimes](https://github.com/defaultnamehere/cookie_crimes) module - full credit for their awesome work!

 This C# project will dump cookies for all sites, even those with httpOnly/secure/session flags.

 ![Running](https://raw.githubusercontent.com/m0rv4i/SharpCookieMonster/master/images/running.png)

 ![Cookies](https://raw.githubusercontent.com/m0rv4i/SharpCookieMonster/master/images/cookies.png)

 ## Usage
 
 Simply run the binary.

 ```
 SharpCookieMonster.exe [https://sitename.com] [chrome-debugging-port] [user data dir]
 ```

An optional first argument sepcifies the site that chrome will initially connect to when launched (default https://www.google.com).
An optional second argument specifies the port to launch the chrome debugger on (by default 9142).
An optional third argument specifies the path to the user data directory, which can be overridden in order to access different profiles etc (default %APPDATALOCAL%\Google\Chrome\User Data).

 ## Building

The binary has been built to be compatible with .NET 3.5 in order to be compatible with victims with older versions of .NET installed. However in order to use WebSockets to communicate with Chrome the WebSocket4Net package was added.

If you want to run this down C2 such as using [PoshC2](https://github.com/nettitude/PoshC2)'s `sharpcookiemonster` command or via CobaltStrike's `execute-assembly` then use ILMerge to merge the built executable with the WebSocket4Net.dll library.

First rename the original binary then run:

```
 ILMerge.exe /targetplatform:"v2,C:\Windows\Microsoft.NET\Framework\v2.0.50727" /out:SharpCookieMonster.exe SharpCookieMonsterOriginal.exe WebSocket4Net.dll 
 ```
