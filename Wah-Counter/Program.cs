using Telegram.Bot;
using Telegram.Bot.Types.Enums;

// I want to make this bot as SIMPLE as it can possibly be.
// You can build the program with `dotnet build` for Windows, Mac or Linux if you have .NET SDK installed.

// You can get an API key from Telegram bot @BotFather, simply /start with it and it will walk you through the steps.
// The bot must have /setprivacy DISABLED. This allows the bot to see messages that do not start with a / like /start
// We need this so the bot can see normal messages like stickers.

// Store all data files in the data directory.
Environment.CurrentDirectory += Path.DirectorySeparatorChar + "data";

HashSet<long> admins = new HashSet<long>();

if (!File.Exists("admins.txt"))
{
    File.Create("admins.txt").Close();
}

foreach (var adminId in File.ReadAllLines("admins.txt"))
{
    admins.Add(long.Parse(adminId.Trim()));
}

if (!File.Exists("stickers.txt"))
{
    File.WriteAllLines("stickers.txt",
        // This is the ID for the original bouncing red panda sticker.
        ["CAACAgEAAxkBAAMGaVbDJf0onqbq2omi5FCcJh-u-A0AAlIFAAIZXTBFIoIPjpIdJ-U4BA"]);
}

// This is the file IDs of the dancing WAH stickers, basically stickers in Telegram are just webp image files that get sent
// around. Below in the OnMessage block I use this to check if the message contains an image with the same file ID.
HashSet<string> dancingWahStickerIds = new HashSet<string>();

foreach (var stickerId in File.ReadAllLines("stickers.txt"))
{
    dancingWahStickerIds.Add(stickerId);
}

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
    "WAH! A new record of {0} red pandas in a row!",
    "The conga line is now {0} pandas long! Keep the WAH going!",
    "Un-wah-lievable! We just hit {0}!",
    "That's a lot of beans! {0} red pandas reached!",
    "The tail-to-snoot ratio is off the charts at {0}!",
    "Maximum fluff achieved: {0} pandas deep!",
    "The Red Panda Train just pulled into {0}-Station!",
    "WAH WAH WAH! That's {0} for the record books!",
    "Is this a conga line or a red panda invasion? {0} and counting!",
    "Alert the Firefox devs! We hit a cache of {0} pandas!",
    "Our bamboo budget can't handle {0} pandas!!",
    "The Wah-nderful Conductor approves of this {0} streak!",
    "Stop! Hammer-WAH! {0} reached!",
    "That’s {0} servings of bamboo for the crew!",
    "A new legend is born at {0} pandas long!",
    "We are reaching critical levels of orange fluff: {0}!",
    "Who needs a job when you have a {0} panda streak?",
    "WAH-tomic level achievement unlocked: {0}!",
    "The Great Wah-ll of China has nothing on this {0} line!",
    "Snoots were booped, stickers were sent: {0}!",
    "Do you hear the pandas sing? Singing the song of {0}!",
    "This conga line is {0} times better than anything else today!",
    "Error 404: Line too long. Just kidding, it's {0}!",
    "The prophecy foretold a line of {0}. It is here!",
    "Absolute Wah-ndemonium at {0}!",
    "To infinity and be-WAH-nd! {0}!",
    "That’s {0} red pandas. My eyes... they see only orange.",
    "The council of fluffy tails is pleased with {0}.",
    "WAH! That's over 9000! (Wait, no, it's {0}.)",
    "Keep that tail wagging! {0} reached!",
    "One small step for panda, one giant leap for WAH: {0}!",
    "If we reach {0}, do we get free bamboo?",
    "The line is so long it's starting to curve! {0}!",
    "I’ve never seen so much Ailurus fulgens in one place: {0}!",
    "WAH-ndrous! Simply Wah-ndrous! {0}!",
    "We’re gonna need a bigger bamboo forest for these {0} pandas.",
    "Is this legal? {0} stickers in a row?!",
    "Pure, unadulterated WAH: {0}!",
    "The spirit of the red panda is strong in this {0} line!",
    "The snoot-to-tail connectivity is 100% at {0}!",
    "WAH! I’m shaking! {0} pandas!",
    "Witness the fitness of this {0} panda line!",
    "Can we get much higher? (WAH-oh-oh) {0}!",
    "This is the peak of furry culture. {0} pandas.",
    "I haven't seen a streak this clean since the Great Bamboo Shortage: {0}!",
    "WAH! That’s a lot of red! {0}!",
    "The Conga-Line Conductor is doing a happy dance for {0}!",
    "Another link in the fluffy chain: {0}!",
    "The red pandas are taking over the chat! {0} and rising!",
    "I'm not crying, you're crying. It's just so beautiful. {0}!",
    "Keep the WAH-mentum going! {0}!",
    "By the power of Grayskull... I mean, Grey-WAH! {0}!",
    "This line is more stable than my code: {0}!",
    "WAH-t are the odds?! {0}!",
    "A new challenger approaches? No, just {0} pandas.",
    "This is {0} times more fluff than legally allowed.",
    "The pawb-power is real at {0}!",
    "Red Panda Conga Level: God-Tier ({0})",
    "The bamboo is strong with this one. {0}!",
    "WAH! We’re making history, one sticker at a time: {0}!",
    "If you break the line now, I’m telling the admin. {0}!",
    "The world is a better place with {0} red pandas in a row.",
    "Look at all those chickens! Wait, those are pandas. {0}!",
    "The 'Wah' is loud in this chat tonight: {0}!",
    "We’ve reached {0}. I can smell the bamboo from here.",
    "My Rust code is sweating trying to count this high: {0}!",
    "WAH! Don't stop me now! {0}!",
    "The fluffy tail club gives this a {0} out of {0}!",
    "You guys are absolute legends. {0}!",
    "Is this the real life? Is this just Wah-ntasy? {0}!",
    "The red panda conga line is officially {0} stickers long!",
    "Prepare for WAH-p speed! {0}!",
    "This streak is {0} days of luck for the whole group!",
    "The panda-monium continues! {0}!",
    "I've got {0} problems but a broken line ain't one!",
    "WAH-hoo! {0} reached!",
    "The Red Rail is moving fast! {0} cars long!",
    "That’s {0} snoots to boop. Get busy!",
    "Our high score just got an upgrade: {0}!",
    "Long panda is looooooooong: {0}!",
    "WAH-t a time to be alive! {0}!",
    "The sticker-to-text ratio is perfect at {0}!",
    "I’m putting this record on my resume: {0}!",
    "The fluff is overflowing! {0}!",
    "Stay orange, stay fluffy, stay {0}!",
    "WAH-nderwall? No, Wah-nder-line! {0}!",
    "The bamboo spirits are singing for our {0} streak!",
    "I’ve calculated the WAH-locity: {0} stickers per hour!",
    "Maximum Red Panda Energy: {0}!",
    "The conga line is {0}. My life is complete.",
    "WAH! That's a Spicy Meat-Wah! {0}!",
    "We’re going to the moon! {0} pandas deep!",
    "The ultimate fluffy flex: {0}!",
    "WAH! Our legacy is cemented at {0}!",
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
        if (scoreParts.Length != 3)
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
        // If the message being received has no author, don't do anything.
        // This is impossible, but .NET won't compile the program unless this is checked.
        if (message.From is null)
            return;

        long fromId = message.From.Id;

        // This is the ID of the group chat or individual chat with the bot.
        // This allows the bot to support more than one group chat and not get confused.
        long chatId = message.Chat.Id;

        Console.WriteLine($"message.Chat.Type: {message.Chat.Type}");
        Console.WriteLine($"message.Chat.Id: {message.Chat.Id}");
        Console.WriteLine($"message.From.Id: {message.From?.Id.ToString() ?? "NULL"}");

        // If this is a private chat with the bot, the admin can manage the bot.
        if (message.Chat.Type == ChatType.Private)
        {
            if (!admins.Contains(fromId))
            {
                await bot.SendMessage(fromId,
                    "OwO? What’s this? Your paws don't have the permissions to touch this button! This command is for big-paws admins only! No touchie the forbidden bamboo! 🐾");
                return;
            }

            string incomingMsg = message.Text?.Trim() ?? string.Empty;
            incomingMsg = incomingMsg.ToLowerInvariant();

            string responseMsg;

            if (message.Sticker is not null)
            {
                string stickerFileId = message.Sticker.FileId;

                if (dancingWahStickerIds.Contains(stickerFileId))
                {
                    dancingWahStickerIds.Remove(stickerFileId);
                    responseMsg =
                        "❌ Hmph! This sticker has been banished from the conga line! Too much chaos, not enough WAH! OwO";
                }
                else
                {
                    dancingWahStickerIds.Add(stickerFileId);
                    responseMsg =
                        "✅ A new panda has joined the parade! This sticker is now officially part of the conga line! WAH!";
                }

                // Save all the stickers to the sticker txt
                File.Delete("stickers.txt");
                File.WriteAllLines("stickers.txt", dancingWahStickerIds);
            }
            else
            {
                switch (incomingMsg)
                {
                    case "/list":
                        foreach (var sticker in dancingWahStickerIds)
                        {
                            await bot.SendSticker(chatId, sticker);
                        }

                        responseMsg = "Lookie lookie at all da fluffy fwiends ready to march\\! OwO";
                        break;
                    default:
                        responseMsg = "Hello, send me a sticker and I will enable/disable its use in the conga line.\n/list to list all stickers";
                        break;
                }
            }

            Console.WriteLine(responseMsg);
            await bot.SendMessage(chatId, responseMsg);
        }
        else
        {
            // This object stores the counts.
            Counter? counts;

            // See if we've seen this group before.
            if (!wahCounts.TryGetValue(chatId, out counts))
            {
                // We have not seen this group before, add it.
                counts = new Counter();
                wahCounts.Add(chatId, counts);
            }

            Console.WriteLine($"Sticker received: {message.Sticker?.FileId ?? "NULL"}");

            // If the message being sent into the group chat is the dancing wah sticker
            if (message.Sticker is not null && dancingWahStickerIds.Contains(message.Sticker.FileId))
            {
                // Increment the Wah Count
                Console.WriteLine("I see a dancing WAH!");

                // This stores the users who have entered the conga line, if a user enters the line more than once it
                // doesn't break the line, but it doesn't count towards a new high score.
                // This only stores unique red pandas who put a sticker in the line.
                counts.uniqueRedPandasInLine.Add(fromId);

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
                    File.Delete("highscores.txt");
                    File.WriteAllLines("highscores.txt",
                        wahCounts.Select(c => $"{chatId}|{counts.highScore}|{counts.nextVictoryMessage}"));
                }
                // If the line ends, but there aren't enough unique red pandas in the line. This message will be sent.
                else if (counts.numberOfWAHs > counts.highScore)
                {
                    await bot.SendMessage(message.Chat,
                        $"There were {counts.numberOfWAHs} dancing WAHs in the line which would have been a new high score, however, too many red pandas put the sticker in multiple times! That doesn't count towards the conga line!!!");
                }
                // No high score was broken, but after three in a line, print stats.
                else if (counts.uniqueRedPandasInLine.Count >= 3)
                {
                    await bot.SendMessage(message.Chat,
                        $"❌ LINE STATUS: SHATTERED\n📉 FINAL STREAK: {counts.uniqueRedPandasInLine.Count} \n🏆 ALL-TIME PEAK: {counts.highScore}");
                }

                // Reset the conga line!
                counts.numberOfWAHs = 0;
                counts.uniqueRedPandasInLine.Clear();
            }
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