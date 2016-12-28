namespace Coolector.Services.Remarks.Modules
{
    public class HomeModule : ModuleBase
    {
        public HomeModule()
        {
            Get("", args => "Welcome to the Coolector.Services.Remarks API!");
        }
    }
}