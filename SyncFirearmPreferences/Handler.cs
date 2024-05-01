using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

namespace SyncFirearmPreferences
{
    internal class Handler
    {
        public void OnSpawning(SpawningEventArgs ev)
        {
            if (Plugin.CachedPreferences.ContainsKey(ev.Player.Id))
            {
                Utilities.ClearSyncPreference(Plugin.CachedPreferences[ev.Player.Id], ev.Player);
            }
        }
    }
}
