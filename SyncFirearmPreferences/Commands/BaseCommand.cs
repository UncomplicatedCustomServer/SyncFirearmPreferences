using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Utf8Json.Resolvers.Internal;

namespace SyncFirearmPreferences.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    internal class BaseCommand : ParentCommand
    {
        public BaseCommand() => LoadGeneratedCommands();

        public override string Command => "sfp";

        public override string Description => "Sync your fiream preferences with the one on the cloud";

        public override string[] Aliases => new string[] { };

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player Sender = Player.Get(sender);

            if (Sender == null)
            {
                response = "Nop";
                return false;
            }

            string KitName = "";
            // I'm clever so we try to get the kit name here
            if (arguments.Count > 1)
            {
                KitName = arguments.At(1);
            }

            if (arguments.Count == 0 || (arguments.Count == 1 && arguments.At(0) == "help"))
            {
                string PasswordWarning = ">> You are correctly logged it, at least the plugin have your password <<";
                if (!Utilities.HasPassword(Sender))
                {
                    PasswordWarning = ">> YOU ARE NOT LOGGED IN! - Please log in with .sfp password <password> <<";
                }
                response = $"Help page for SyncFirearmPreferences\n\n{PasswordWarning}\n\n.sfp password <password> => Log in or set a new password\n.sfp sync (kit name) => Change your current firearm preferences based on the cloud's one\n.sync put (kit name) => Change the firearm preferences on the cloud based on your current ones\n.sync switch => Switch the sync inventory from server local (based on server) or global. Current: {Utilities.GetSyncPreference(Sender)}";
                return true;
            } 
            else if (arguments.At(0) == "password")
            {
                if (Utilities.HasPassword(Sender))
                {
                    response = "You are already logged-in!";
                    return true;
                }

                if (arguments.Count < 2)
                {
                    response = "Sorry but to use the 'password' command you need at least another argument which is the password.\nDon't use the same password for everything btw";
                    return false;
                } 
                else
                {
                    HttpStatusCode Status = Utilities.ProposePassword(Sender, arguments.At(1), out _);
                    if (Status == HttpStatusCode.OK)
                    {
                        response = $"Successfully logged in into the cloud as {Sender.UserId}!";
                        Plugin.Passwords.Add(Sender.Id, Utilities.EncodedPassword(arguments.At(1)));
                    }
                    else if (Status == HttpStatusCode.Created)
                    {
                        response = "Successfully registered the password into the cloud. Remember it to continue to use the service!";
                        Plugin.Passwords.Add(Sender.Id, Utilities.EncodedPassword(arguments.At(1)));
                    }
                    else
                    {
                        response = "Wrong password! Please try again!";
                    }
                    return true;
                }
            } 
            else if (arguments.At(0) == "sync")
            {
                if (!Utilities.HasPassword(Sender))
                {
                    response = "Sorry but you need to set a password before by doing .sfp password <password>\nNOTICE: Avoid using the same password, this password might be seen by server owners also if it's encrypted as soon as the plugin get his hands on it!";
                    return true;
                }

                if (Utilities.SyncPreferencesFromServer(Sender, out string ResponseData, KitName))
                {
                    response = "Your firearm preference list has successfully been synced with the one on the cloud!\nEnjoy your settings!";
                }
                else
                {
                    Dictionary<string, string> Response = JsonConvert.DeserializeObject<Dictionary<string, string>>(ResponseData);
                    response = $"Failed to sync your firearm preferences from the cloud.\nAPI says: {Response["message"]} ({Response["status"]})";
                }
                return true;
            } 
            else if (arguments.At(0) == "put" || arguments.At(0) == "update")
            {
                if (!Utilities.HasPassword(Sender))
                {
                    response = "Sorry but you need to set a password before by doing .sfp password <password>\nNOTICE: Avoid using the same password, this password might be seen by server owners also if it's encrypted as soon as the plugin get his hands on it!";
                    return true;
                }

                if (Utilities.PutPreferencesOnServer(Sender, out HttpContent content, KitName))
                {
                    response = "Successfully put your preferences on the cloud!";
                } 
                else
                {
                    Task<string> ResponseTask = Task.Run(content.ReadAsStringAsync);
                    ResponseTask.Wait();

                    Dictionary<string, string> Response = JsonConvert.DeserializeObject<Dictionary<string, string>>(ResponseTask.Result);

                    response = $"Failed to sync the cloud firearm with your current preferences.\nAPI says: {Response["message"]} ({Response["status"]})";
                }
                return true;
            } 
            else if (arguments.At(0) == "switch")
            {
                if (!Utilities.HasPassword(Sender))
                {
                    response = "Sorry but you need to set a password before by doing .sfp password <password>\nNOTICE: Avoid using the same password, this password might be seen by server owners also if it's encrypted as soon as the plugin get his hands on it!";
                    return true;
                }

                if (Utilities.GetSyncPreference(Sender) == SyncPreference.Global)
                {
                    Plugin.PlayerPreferences[Sender.Id] = SyncPreference.Local;
                }
                else
                {
                    Plugin.PlayerPreferences[Sender.Id] = SyncPreference.Global;
                }
                response = $"Successfully updated the preference for the selection! Current: {Utilities.GetSyncPreference(Sender)}";
                return true;
            } 
            else
            {
                response = "The subcommand was not found!";
                return false;
            }
        }
    }
}
