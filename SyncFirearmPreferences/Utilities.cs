using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Structs;
using Exiled.Loader.GHApi;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.BasicMessages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SyncFirearmPreferences
{
    internal class Utilities
    {
        public static SyncPreference GetSyncPreference(Player player)
        {
            if (Plugin.PlayerPreferences.ContainsKey(player.Id))
            {
                return Plugin.PlayerPreferences[player.Id];
            }
            return SyncPreference.Global;
        }

        public static string SyncPreferenceToStringBool(SyncPreference syncPreference)
        {
            if (syncPreference == SyncPreference.Global)
            {
                return "false";
            }
            return "true";
        }

        public static string EncodedPassword(string password)
        {
            return BitConverter.ToString(SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        public static bool HasPassword(Player player)
        {
            return Plugin.Passwords.ContainsKey(player.Id);
        }

        public static string GetPassword(Player player)
        {
            if (Plugin.Passwords.ContainsKey(player.Id))
            {
                return Plugin.Passwords[player.Id];
            }
            return "";
        }

        public static bool PutPreferencesOnServer(Player player, out HttpContent content, string kitName = "")
        {
            Dictionary<string, List<string>> Data = new();
            foreach (KeyValuePair<FirearmType, AttachmentIdentifier[]> Elements in player.Preferences)
            {
                List<string> Attachments = new();
                foreach (AttachmentIdentifier AttachmentIdentifier in Elements.Value)
                {
                    Attachments.Add(AttachmentIdentifier.Name.ToString());
                }

                Data.Add(Elements.Key.ToString(), Attachments);
            }



            Task<HttpResponseMessage> Task = System.Threading.Tasks.Task.Run(() => Plugin.HttpClient.PostAsync($"https://ucs.fcosma.it/api/sfp/put?player={player.UserId}&password={GetPassword(player)}&local={SyncPreferenceToStringBool(GetSyncPreference(player))}&server={Server.IpAddress}@{Server.Port}&kit={kitName}", new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "content", JsonConvert.SerializeObject(Data) }
            })));

            Task.Wait();

            content = Task.Result.Content;

            if (Task.Result.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }

        public static HttpStatusCode ProposePassword(Player player, string password, out HttpContent content)
        {
            Task<HttpResponseMessage> Task = System.Threading.Tasks.Task.Run(() => Plugin.HttpClient.GetAsync($"https://ucs.fcosma.it/api/sfp/login?player={player.UserId}&password={EncodedPassword(password)}"));
            Task.Wait();

            content = Task.Result.Content;

            return Task.Result.StatusCode;
        }

        public static bool RetrivePreferencesFromServer(Player player, out HttpContent content, string kitName = "")
        {
            Task<HttpResponseMessage> Task = System.Threading.Tasks.Task.Run(() => Plugin.HttpClient.GetAsync($"https://ucs.fcosma.it/api/sfp/retrive?player={player.UserId}&local={SyncPreferenceToStringBool(GetSyncPreference(player))}&server={Server.IpAddress}@{Server.Port}&kit={kitName}"));
            Task.Wait();

            content = Task.Result.Content;

            if (Task.Result.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public static bool SyncPreferencesFromServer(Player player, out string response, string kitName = "")
        {
            if (RetrivePreferencesFromServer(player, out HttpContent content, kitName))
            {
                Task<string> Content = Task.Run(content.ReadAsStringAsync);
                Content.Wait();

                response = Content.Result;

                Dictionary<string, List<string>> Data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(response);

                if (Plugin.CachedPreferences.ContainsKey(player.Id))
                {
                    Plugin.CachedPreferences[player.Id] = Data;
                }
                else
                {
                    Plugin.CachedPreferences.Add(player.Id, Data);
                }

                if (Data.Count < 1)
                {
                    response = "{\"status\":404,\"message\":\"No configuration found!\"}";
                    return false;
                }

                ClearSyncPreference(Data, player);

                return true;
            } 
            else
            {
                response = null;
                Log.Warn("Unable to retrive the data from the server!");
                return false;
            }
        }

        public static void ClearSyncPreference(Dictionary<string, List<string>> Data, Player player)
        {
            ClearPreferences(player);

            foreach (KeyValuePair<string, List<string>> Entry in Data)
            {
                if (Enum.TryParse(Entry.Key, out FirearmType Firearm))
                {
                    List<AttachmentIdentifier> Attachments = new();
                    foreach (string Attachment in Entry.Value)
                    {
                        if (Enum.TryParse(Attachment, out AttachmentName Name))
                        {
                            Attachments.Add(AttachmentIdentifier.Get(Firearm, Name));
                        }
                        else
                        {
                            Log.Error($"Failed to parse the AttchNm {Attachment}");
                        }
                    }

                    AddPreference(player, Firearm, Attachments.ToArray());
                }
                else
                {
                    Log.Error($"Failed to parse the firearm {Entry.Key}!");
                }
            }
        }

        // Copied from EXILED's Firearm.cs because they are dumb
        public static void AddPreference(Player player, FirearmType itemType, AttachmentIdentifier[] attachments)
        {
            foreach (KeyValuePair<Player, Dictionary<FirearmType, AttachmentIdentifier[]>> kvp in Firearm.PlayerPreferences)
            {
                if (kvp.Key != player)
                    continue;

                if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(player.ReferenceHub, out Dictionary<ItemType, uint> dictionary))
                    dictionary[itemType.GetItemType()] = attachments.GetAttachmentsCode();
            }
        }

        // Copied from EXILED's Firearm.cs because they are dumb
        public static void RemovePreference(Player player, FirearmType type)
        {
            foreach (KeyValuePair<Player, Dictionary<FirearmType, AttachmentIdentifier[]>> kvp in Firearm.PlayerPreferences)
            {
                if (kvp.Key != player)
                    continue;

                if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(player.ReferenceHub, out Dictionary<ItemType, uint> dictionary))
                    dictionary[type.GetItemType()] = type.GetBaseCode();
            }
        }

        // Copied from EXILED's Firearm.cs because they are dumb
        public static void ClearPreferences(Player player)
        {
            foreach (KeyValuePair<FirearmType, AttachmentIdentifier[]> Data in Firearm.PlayerPreferences[player])
            {
                RemovePreference(player, Data.Key);
            }
        }
    }
}
