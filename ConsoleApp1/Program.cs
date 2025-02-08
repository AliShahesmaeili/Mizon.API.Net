// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Mizon.API;

Console.WriteLine("Hello, World!");


var client = new MizonApi();

await client.SendRequestAsync(new LoginMethod(new() { Username = "ali", Password = "aaa" }));


await client.SendRequestAsync(new LoginMethod(new() { Username = "ali", Password = "aaa" }));