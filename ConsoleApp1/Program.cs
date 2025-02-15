// See https://aka.ms/new-console-template for more information
using ConsoleApp1;
using Mizon.API;

Console.WriteLine("Hello, World!");


var client = new MizonApi();

var model1 = new LoginMethod( new() { Username = "ali", Password = "aaa" });

var model2 = new LoginMethod(new () { Username = "ali", Password = "aaa" });



Console.WriteLine("Hello, World!");

await client.SendRequestAsync(model1);
await client.SendRequestAsync(model2);

