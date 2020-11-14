using System.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Examples.Echo
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static string userChatId = "";
        private static string botToken = "";

        private static int sshPid;

        private static string user;
        private static string remoteIp;
        private static string port;
        private static string thisIpv4;
        private static string thisIpv6;
        private static string hostName;

        private static DateTime attemptTime;

        private static string memUsed;
        private static string memTotal;

        private static bool working = true;

        public static async Task Main(string[] args)
        {
            if (args[0] == "install")
            {
                installScript();
            }

            // Fill UNIX command based variables
            hostName = "printf $HOSTNAME".Bash();
            sshPid = int.Parse("ps --no-headers -fp $$ | awk '{print $3}'".Bash());

            user = "printf $USER".Bash();
            port = "printf $SSH_CLIENT | awk '{ print $3}'".Bash();

        	remoteIp = "printf $SSH_CLIENT | awk '{ print $1 }'".Bash();
        	thisIpv4 = "dig TXT +short o-o.myaddr.l.google.com @ns1.google.com -4 | awk -F'\"' '{ print $2}'".Bash();
        	thisIpv6 = "dig TXT +short o-o.myaddr.l.google.com @ns1.google.com -6 | awk -F'\"' '{ print $2}'".Bash();

            attemptTime = DateTime.Now;
            memTotal = "free -m | grep -oP '\\d+' | head -n 1".Bash();
            memUsed = "free -m | grep -oP '\\d+' | head -n 3 | sed -n 2p".Bash();

            if (args[0] == "welcome")
            {
                writeWelcome();
            }

            userChatId = args[0];
            botToken = args[1];

            Console.WriteLine($"Confirm login on Telegram ...\n");

            Bot = new TelegramBotClient(botToken);

            // Prepare all events
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());

            sendGeneralMessage();

            while (working)
            {
                Console.ReadLine();
            }

            Bot.StopReceiving();
        }

        private static void writeWelcome()
        {
            Console.WriteLine($"Welcome on {hostName} SSH server.");
            Console.WriteLine($"");
            Console.WriteLine($"Server IPv4: {thisIpv4}\nServer IPv6: {thisIpv6}");
            Console.WriteLine($"");
            Console.WriteLine($"Server time: {DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}");
            Console.WriteLine($"Server is {"uptime -p".Bash()}");
            Console.WriteLine($"");
            Console.WriteLine($"Connecting from: {remoteIp}");
            Console.WriteLine($"");
            Console.WriteLine($"Memory usage: {memUsed}/{memTotal}Mb");
            Console.WriteLine($"CPU Load: {"uptime | awk -F'[a-z]:' '{ print $2}'".Bash()}");
            Environment.Exit(0);
        }

        private static void installScript()
        {
            bool hasRootAccess = "printf $EUID".Bash() == "0";

            if (hasRootAccess)
            {
                Console.WriteLine("Notice: I'm not responsible for being locked out of SSH, broken servers, thermonuclear war, or whatever unexpected may happen. You decided to install this so take your responsibility!");
                Console.WriteLine("Thank you for downloading my script! (You can read the full source code over at GitHub: )");


                string profile =    "trap '' 2" +
                                    "/usr/bin/ssh-report CHAT_ID BOT_TOKEN" +
                                    "if [ \"$?\" -eq \"40\" ]; then" +
                                    "    /usr/bin/ssh-report welcome" +
                                    "    trap 2" +
                                    "else" +
                                    "    kill -9 $PPID" +
                                    "fi";

                Console.WriteLine("Adding trap script to start of /etc/profile ...");
                System.IO.File.WriteAllText("/etc/profile", profile + Environment.NewLine + System.IO.File.ReadAllText("/etc/profile"));

                Console.WriteLine("Copying ssh-report to /usr/bin/ssh-report ...");
                System.IO.File.Copy(Environment.CurrentDirectory + "/ssh-report", "/usr/bin/ssh-report");
                
                Console.WriteLine("Allowing execution to /usr/bin/ssh-report ...");
                "sudo chmod +x /usr/bin/ssh-report".Bash();

                Console.WriteLine("Done! Try logging in to SSH with another terminal/tab.");
                Console.WriteLine("WARNING!!! Better leave this terminal open until you confirmed it to be working!!!");
            }
            else
                Console.WriteLine("Run this as sudo!");

            Environment.Exit(0);
        }

        private static async void sendGeneralMessage()
        {
            await Bot.SendChatActionAsync(userChatId, ChatAction.Typing);
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                // first row
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Allow SSH login", "/allow"),
                    InlineKeyboardButton.WithCallbackData("Disallow SSH login", "/disallow"),
                },
                // second row
                new []
                {
                    InlineKeyboardButton.WithCallbackData("\U000026A0 Disallow and ban IP \U000026A0", "/disallowandban"),
                }
            });

            await Bot.SendTextMessageAsync(
                chatId: userChatId,
                text: $"\U000026A0 Someone is trying to login to SSH (Port: {port})!\n\n\t\tServer: {hostName}\n\n\t\tLogin: {user}\n\t\tRequester IP: {remoteIp}\n\n\t\tServer IPv4: {thisIpv4}\n\t\tServer IPv6: {thisIpv6}\n\nDo you want to grant shell access?",
                replyMarkup: inlineKeyboard
            );
        }

        // Process Inline Keyboard callback data
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            if (callbackQuery.Data == "/disallow")
            {
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"\U0001F44D \U000026D4 login for user '{user}' logging in from '{remoteIp}' declined!"
                );
                working = false;
                Environment.Exit(500);
            }
            else if (callbackQuery.Data == "/allow")
            {
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"\U0001F44D \U00002714 login for user '{user}' logging in from '{remoteIp}' allowed!"
                );
                working = false;
                Environment.Exit(9000);
            }
            else if (callbackQuery.Data == "/disallowandban")
            {
                await Bot.SendChatActionAsync(userChatId, ChatAction.Typing);
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Proceed", "/disallowandban-confirm"),
                        InlineKeyboardButton.WithCallbackData("Cancel", "/disallowandban-cancel"),
                    }
                });

                await Bot.SendTextMessageAsync(
                    chatId: userChatId,
                    text: $"\U000026A0 Someone is trying to login to SSH (Port: {port})!\n\n\t\tLogin: {user}\n\t\tRequester IP: {remoteIp}\n\n\t\tServer IPv4: {thisIpv4}\n\t\tServer IPv6: {thisIpv6}\n\nDo you want to grant shell access?",
                    replyMarkup: inlineKeyboard
                );
            }
            else if (callbackQuery.Data == "/disallowandban-confirm")
            {
                $"echo 'ALL: {remoteIp}' | sudo tee -a /etc/hosts.deny".Bash();
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"\U0001F44D \U00002714 '{remoteIp}' is now banned!"
                );
                working = false;
                Environment.Exit(451);
            }
            else if (callbackQuery.Data == "/disallowandban-cancel")
            {
                sendGeneralMessage();
            }
        }

        #region Inline Mode

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}