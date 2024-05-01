using Exiled.API.Enums;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Net.Http;
using PlayerEventSource = Exiled.Events.Handlers.Player;

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

        internal static Dictionary<int, string> Passwords;

        public static Dictionary<int, Dictionary<string, List<string>>> CachedPreferences;

        public static HttpClient HttpClient;

        internal Handler Handler;

        public override void OnEnabled()
        {
            PlayerPreferences = new();
            Passwords = new();
            CachedPreferences = new();
            HttpClient = new();

            Handler = new();

            PlayerEventSource.Spawning += Handler.OnSpawning;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerEventSource.Spawning -= Handler.OnSpawning;

            Handler = null;

            base.OnDisabled();
        }
    }
}
