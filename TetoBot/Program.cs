﻿// See https://aka.ms/new-console-template for more information

using TetoBot;

Console.WriteLine("Hello, World!");

await using var bot = new Bot();
await bot.RunAsync();
await Task.Delay(-1);