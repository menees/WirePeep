 [![Latest Release](https://img.shields.io/github/v/release/menees/WirePeep)](https://github.com/menees/WirePeep/releases)
![windows build](https://github.com/menees/Hasher/workflows/windows%20build/badge.svg)

![WirePeep Icon](src/WirePeep/Images/WirePeep.svg)
# WirePeep
WirePeep is a free, open-source network downtime monitor written in C# with WPF, .NET Core, and .NET Framework. It periodically checks for connectivity to your Internet gateway, various public DNS servers, and other systems (e.g., your cable modem). When it can't connect to any system in a peer group for a configurable fail time, it alerts that the group is inaccessible.

I wrote this during the spring of COVID-19 because I had so many intermittent network outages while working at home. My Internet provider blamed my cable modem, my wiring, my neighbors, sunspots, gremlins, etc. After weeks of logging outages, calling my ISP repeatedly, and having multiple techs visit the house, I finally got a maintenance tech that replaced the amplifier and tap at the street, and all my connectivity problems went away. So hooray for persistence and having the data to document my outages.

I made this utility generic enough where I can also use it at work to monitor our primary and secondary Internet connections as well as various other critical internal systems (e.g., domain controllers, firewalls, managed switches, wireless hubs). I put all the backend logic in a separate WirePeep.Common library, so I can use it from a console app or service someday. All the UI logic and WPF dependencies are in the WirePeep executable project, so I can replace it with something else "easily' (e.g., a [.NET MAUI](https://github.com/dotnet/maui) front-end in a few years).

The name WirePeep was one of many I considered, but it was the only one that had no significant hits on DuckDuckGo, Bing, or Google. Also, the wirepeep.com domain name was available, so that was a plus.

![ScreenShot](http://www.menees.com/Images/WirePeep.png)