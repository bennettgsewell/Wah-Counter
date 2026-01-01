using Telegram.Bot;

// I want to make this bot as SIMPLE as it can possibly be.
// You can build the program with `dotnet build` for Windows, Mac or Linux if you have .NET SDK installed.

// You can get an API key from Telegram bot @BotFather, simply /start with it and it will walk you through the steps.
// The bot must have /setprivacy DISABLED. This allows the bot to see messages that do not start with a / like /start
// We need this so the bot can see normal messages like stickers.

// This is the file ID of the dancing WAH sticker, basically stickers in Telegram are just webp image files that get sent
// around. Below in the OnMessage block I use this to check if the message contains an image with the same file ID.
const string dancingWahStickerId = "CAACAgEAAxkBAAMGaVbDJf0onqbq2omi5FCcJh-u-A0AAlIFAAIZXTBFIoIPjpIdJ-U4BA";

// We need to get the bot API key into this program;
// You can either pull the API key for the bot from an environment variable.
// `TELEGRAM_BOT_API_KEY=123456789 ./Wah-Counter`
string? apiKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_API_KEY");
// Or if the environment variable isn't set it will look for a file in the same directory as the program.
// If you make a file called api.key and paste the api key in it, it should load it up.
if (apiKey is null && File.Exists("api.key"))
{
    apiKey = File.ReadAllText("api.key").Trim();
}

// We couldn't find the API key, write error messages and exit.
if (apiKey is null)
{
    Console.Error.WriteLine("Api key not found");
    Console.Error.WriteLine("Please set ENV VAR \"TELEGRAM_BOT_API_KEY\"");
    Console.Error.WriteLine("Or store the key in a file called \"api.key\"");
    return 1;
}

string[] funnyMessages =
[
    // These messages will be released linearly when new high score is reached, the {0} is replaced with the new
    // high score value.
    "A {0}",
    "B {0}",
    "C {0}",
    "D {0}",
    "E {0}",
];

// This library is from:
// https://www.nuget.org/packages/telegram.bot/
// https://github.com/TelegramBots/Telegram.Bot
// I've used it several times and peered through the source code, it's just a simple wrapper for all the bots HTTP
// calls to the Telegram API.
// Cool note, Telegram bots use HTTP calls which stay open until an event occurs. This means you do NOT have to open
// any ports on your firewall! Pretty cool!
TelegramBotClient bot = new TelegramBotClient(apiKey);

// Because this bot could be added to more than one group chat, this will store counts for each individual group.
Dictionary<long, Counter> wahCounts = new();

// Load all the previous high scores from the high score file.
if (File.Exists("highscores.txt"))
{
    string[] lines = File.ReadAllLines("highscores.txt");

    foreach (var line in lines)
    {
        var scoreParts = line.Split('|');
        
        // The line must have three parts
        if(scoreParts.Length != 3)
            continue; // Skip the line, data bad.
        
        // We store three values in each line of the high score file
        // GroupChatId|HighScore|NextVictoryMessage
        if (long.TryParse(scoreParts[0], out long groupChatId)
            && int.TryParse(scoreParts[1], out int highScore)
            && int.TryParse(scoreParts[2], out int nextVictoryMessage))
        {
            wahCounts.Add(groupChatId, new Counter()
            {
                highScore = highScore,
                nextVictoryMessage = nextVictoryMessage,
            });
        }
    }
}

// This prevents the CPU from accessing the same memory with multiple threads, we only allow one message to be 
// processed at a single time.
SemaphoreSlim semaphore = new(1, 1);

// When the bot sees a message, run this block of code.
bot.OnMessage += async (message, _) =>
{
    // We only process one message at a time, wait our turn.
    await semaphore.WaitAsync();

    try
    {
        // This is the ID of the group chat or individual chat with the bot.
        // This allows the bot to support more than one group chat and not get confused.
        long groupChatId = message.Chat.Id;

        // This object stores the counts.
        Counter? counts;

        // See if we've seen this group before.
        if (!wahCounts.TryGetValue(groupChatId, out counts))
        {
            // We have not seen this group before, add it.
            counts = new Counter();
            wahCounts.Add(groupChatId, counts);
        }

        // If the message being sent into the group chat is the dancing wah sticker
        if (message.Sticker?.FileId == dancingWahStickerId)
        {
            // Increment the Wah Count
            Console.WriteLine("I see a dancing WAH!");

            // If the message being received has no author, don't do anything.
            // This is impossible, but .NET won't compile the program unless this is checked.
            if (message.From is null)
                return;

            // This stores the users who have entered the conga line, if a user enters the line more than once it
            // doesn't break the line, but it doesn't count towards a new high score.
            // This only stores unique red pandas who put a sticker in the line.
            counts.uniqueRedPandasInLine.Add(message.From.Id);

            // Increment the count!
            // This stores all the stickers regardless if they're a unique red panda.
            // If the numberOfWAHs is greater than the high score, but they're not unique red pandas.
            // The bot will send a message to chat saying how many stickers were in the line, but how it's not a new
            // high score because they had too many red pandas put in multiple stickers instead of just one.
            counts.numberOfWAHs += 1;
        }
        // The message was not a dancing wah sticker, restart the counter.
        else
        {
            Console.WriteLine("I see no wah here.");

            // If the number of WAHs is higher than the last high score, send a message to the chat!
            if (counts.uniqueRedPandasInLine.Count > counts.highScore)
            {
                // Set the new high score!
                counts.highScore = counts.uniqueRedPandasInLine.Count;

                // Write a message to the terminal
                Console.WriteLine($"New conga line high score reached {counts.highScore}!!!");

                string victoryMessage = funnyMessages[counts.nextVictoryMessage % funnyMessages.Length];

                counts.nextVictoryMessage += 1;

                // Replace the {0} in the message with the new high score.
                string msg = string.Format(victoryMessage, counts.highScore);
                
                // Write a message to the group chat
                await bot.SendMessage(message.Chat, msg);
                
                // Save all the high scores to the high score file.
                // GroupChatId|HighScore|NextVictoryMessage
                File.WriteAllLines("highscores.txt",
                    wahCounts.Select(c => $"{groupChatId}|{counts.highScore}|{counts.nextVictoryMessage}"));
            }
                
            // Reset the conga line!
            counts.numberOfWAHs = 0;
            counts.uniqueRedPandasInLine.Clear();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
    }
    finally
    {
        // We're done processing the message, allow another thread to process the next message.
        semaphore.Release();
    }
};

// Print a message to show the user that it's online.
Console.WriteLine("Wah-Counter bot started!");

// The program will continue running as long as this is true.
bool running = true;

// If the user presses Control-C or the close button on the terminal window, the program will set running to false.
Console.CancelKeyPress += (sender, eventArgs) => { running = false; };

// As long as running is true it will loop, using no CPU, forever.
// This keeps the program running and not closing.
while (running)
{
    Thread.Sleep(TimeSpan.FromSeconds(2));
}

// Exit the program with status code 0 "ALL GOOD"
return 0;

// The object definition which stores the counts.
class Counter
{
    public int highScore = 0;

    /// <summary>
    /// This is the number of dancing stickers added to the conga line, regardless of whether they're a unique user.
    /// </summary>
    public int numberOfWAHs = 0;
    
    /// <summary>
    /// This stores the users who have entered the conga line, if a user enters the line more than once it doesn't
    /// break the line, but it doesn't count towards the high score.
    /// </summary>
    public HashSet<long> uniqueRedPandasInLine = new HashSet<long>();

    /// <summary>
    /// This is the next victory message to send to the group chat when a new high score is reached.
    /// </summary>
    public int nextVictoryMessage = 0;
}