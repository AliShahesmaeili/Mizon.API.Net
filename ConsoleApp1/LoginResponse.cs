﻿using Mizon.API;

namespace ConsoleApp1;

public class LoginResponse : IApiResponse
{
    public string Token { get; set; }
}
