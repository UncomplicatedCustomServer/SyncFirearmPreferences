using Exiled.API.Enums;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncFirearmPreferences
{
    internal class Plugin : Plugin<Config>
    {
        public override string Author => "FoxWorn3365";

        public override string Name => "SyncFirearmPreferences";

        public override string Prefix => "SFP";

        public override Version Version => new(0, 5, 0);

        public override Version RequiredExiledVersion => new(8, 8, 1);

        public override PluginPriority Priority => PluginPriority.Lower;

        public static Dictionary<int, SyncPreference> PlayerPreferences;

        public static Dictionary<int, string> Passwords;

        public static HttpClient HttpClient;

        public override void OnEnabled()
        {
            PlayerPreferences = new();
            Passwords = new();
            HttpClient = new();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
        }
    }
}
