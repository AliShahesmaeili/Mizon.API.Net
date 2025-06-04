namespace Mizon.API.Client;

public class Endpoints
{
    private const string BASE_URL = "https://localhost:7140";
    public const string BASE_HUB_URL = "https://localhost:7140/hub/realtime";


    private const string ACCOUNT_CONTROLER = $"{BASE_URL}/Account";



    public const string LOGIN = $"{ACCOUNT_CONTROLER}/Login";

    public const string ME = $"{ACCOUNT_CONTROLER}/Me";

}
