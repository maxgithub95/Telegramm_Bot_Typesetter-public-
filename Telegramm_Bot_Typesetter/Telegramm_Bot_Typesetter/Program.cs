using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using static System.IO.Path;
using System.IO;


namespace Telegramm_Bot_Typesetter
{
    public class Program
    {
        static ConcurrentDictionary<GameRoom, long> Rooms = new ConcurrentDictionary<GameRoom, long>();
        static ConcurrentStack<User> Users = new ConcurrentStack<User>();
        public static event Func<Task<Message>> NewGameRoom;
        public static event Func<Task<Message>> DeleteGameRoom;
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception.ToString();

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {

                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            long ChatID = update.Message!.Chat.Id;
                            if (!Users.Any(item => item.id == ChatID))
                            {
                                if (update.Message!.Text?.ToLower().Trim(' ') == "/start")
                                {
                                    var userName = $"{update.Message!.From.LastName} {update.Message!.From.FirstName}";
                                    var user = new User(ChatID, botClient, userName);
                                    Users.Push(user);
                                    Console.WriteLine($"Создал User с id ={user.id}");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId: update.Message!.Chat.Id, text: "Для начала работы с ботом напишите /start или выберите \"Приветствие\" в меню слева от поля ввода сообщения");
                                    break;
                                }
                            }
                            User UpdateUserM = Users.Single(item => item.id == ChatID);
                            switch (UpdateUserM.state)
                            {
                                case User.states.Main:
                                    await BotOnMessageReceivedMain(botClient, update.Message!, UpdateUserM);
                                    break;
                                case User.states.Game:
                                    await BotOnMessageReceivedGame(botClient, update.Message!, UpdateUserM);
                                    break;
                            }

                        }
                        break;
                    case UpdateType.CallbackQuery:
                        var ID = update.CallbackQuery.Message.Chat.Id;
                        User? UpdateUserCQ = Users.SingleOrDefault(item => item.id == ID);
                        if (UpdateUserCQ != null)
                        {
                            switch (UpdateUserCQ.state)
                            {
                                case User.states.GameJoin:
                                    await BotOnKeyboardPushAnswerJoinRoom(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                                case User.states.Main:
                                    await BotOnKeyboardPushAnswerMain(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                                case User.states.TrainingMenu:
                                    await BotOnKeyboardPushAnswerTrainingMenu(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                                case User.states.Game:
                                    break;
                                case User.states.GameHost:
                                    await BotOnKeyboardPushAnswerGameHost(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                                case User.states.WaitGame:
                                    await BotOnKeyboardPushAnswerWaitGame(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                                case User.states.Rulles:
                                    await BotOnKeyboardPushAnswerRulles(botClient, update.CallbackQuery, UpdateUserCQ);
                                    break;
                            }
                        }
                        else await botClient.SendTextMessageAsync(chatId: update.CallbackQuery.Message.Chat.Id, text: "Для начала работы с ботом напишите /start или выберите \"Приветствие\" в меню слева от поля ввода сообщения");
                        break;
                    //default:
                    //    await botClient.SendTextMessageAsync(chatId: update.Message!.Chat.Id, text: "Не умею работать с таким типом данных");
                    //    break;
                }


            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }


        private static async Task BotOnMessageReceivedMain(ITelegramBotClient botClient, Message message, User _user)
        {
            var action = message.Text?.ToLower().Trim(' ');
            Console.WriteLine($"Получил сообщение в Главном Меню: {action} от User c id = {_user.id} c состоянием {_user.state}");
            switch (action)
            {
                case "/start":
                    await StartMessageAsync(botClient, _user);
                    await SendInLineKeybordMain(botClient, _user);
                    break;
            }
        }
        private static async Task<Message> SendInLineKeybordMain(ITelegramBotClient botClient, User _user)
        {
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Правила","/rules"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Тренировка","/training"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Создать игровую комнату","/creategame"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Найти игровую комнату","/joingame"),
                    }
                }
                );
            return await botClient.SendTextMessageAsync(chatId: _user.id,
                            text: "Вы находитесь в главном меню: ",
                            replyMarkup: inlineKeyboard);
        }
        private static async Task<Message> SendInLineKeybordMain(ITelegramBotClient botClient, int MessegeId, User _user)
        {
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Правила","/rules"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Тренировка","/training"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Создать игровую комнату","/creategame"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Найти игровую комнату","/joingame"),
                    }
                }
                );
            return await botClient.EditMessageTextAsync(chatId: _user.id,
                            messageId: MessegeId,
                            text: "Вы находитесь в главном меню: ",
                            replyMarkup: inlineKeyboard);
        }
        private static async Task BotOnKeyboardPushAnswerMain(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            switch (callbackQuery.Data)
            {
                case "/rules":
                    _user.state = User.states.Rulles;
                    await SendInLineKeybordRules(botClient, callbackQuery.Message!.MessageId, _user, "/backmain");
                    break;
                case "/training":
                    _user.state = User.states.TrainingMenu;
                    await SendInLineKeybordTrainingMenu(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
                case "/creategame":
                    _user.state = User.states.GameHost;
                    await CreateGameAsync(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
                case "/joingame":
                    _user.state = User.states.GameJoin;
                    await SendInlineKeyboardJoinGame(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
            }
        }
        private static async Task<Message> SendInLineKeybordTrainingMenu(ITelegramBotClient botClient, int messageId, User _user)
        {
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Старт!","/starttraining"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("<= Назад","/back"),
                    }
                }
                );
            return await botClient.EditMessageTextAsync(chatId: _user.id,
                            messageId: messageId,
                            text: "Вы находитесь в меню тренировки. После того как вы нажмете старт, я вышлю Вам Ключевое Слово!\nУ Вас будет ровно минута на набор как можно большего количества слов.",
                            replyMarkup: inlineKeyboard);
        }
        private static async Task BotOnKeyboardPushAnswerTrainingMenu(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            switch (callbackQuery.Data)
            {
                case "/starttraining":
                    await StartTrainingAsync(botClient, _user);
                    break;
                case "/back":
                    _user.state = User.states.Main;
                    await SendInLineKeybordMain(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
            }
        }
        private static async Task<Message> SendInlineKeyboardJoinGame(ITelegramBotClient botClient, int messageId, User _user)
        {
            NewGameRoom = UpdateKeyBoard;
            DeleteGameRoom = UpdateKeyBoard;
            await Task.Delay(300);
            List<InlineKeyboardButton[]> RoomsButton = new List<InlineKeyboardButton[]>();
            foreach (var room in Rooms)
            {
                RoomsButton.Add(new[] { InlineKeyboardButton.WithCallbackData("Подключиться к комнате с id - " + room.Value.ToString(), room.Value.ToString()) });
            }
            RoomsButton.Add(new[] { InlineKeyboardButton.WithCallbackData("<= Назад", "/back") });
            InlineKeyboardMarkup inlineKeyboard = RoomsButton.ToArray();
            return await botClient.EditMessageTextAsync(chatId: _user.id, messageId: messageId,
                                text: "Если на данный момент есть игровые комнаты, вы увидите их в списке ниже.\nКак только будет появляться новая комната, она будет добавлена в список автоматически.",
                                replyMarkup: inlineKeyboard);
            async Task<Message> UpdateKeyBoard()
            {
                return await SendInlineKeyboardJoinGame(botClient, messageId, _user);
            }
        }
        private static async Task<Message> SendInlineKeyboardJoinGame(ITelegramBotClient botClient, User _user)
        {
            await Task.Delay(300);
            List<InlineKeyboardButton[]> RoomsButton = new List<InlineKeyboardButton[]>();
            foreach (var room in Rooms)
            {
                RoomsButton.Add(new[] { InlineKeyboardButton.WithCallbackData("Подключиться к комнате с id - " + room.Value.ToString(), room.Value.ToString()) });
            }
            RoomsButton.Add(new[] { InlineKeyboardButton.WithCallbackData("<= Назад", "/back") });
            InlineKeyboardMarkup inlineKeyboard = RoomsButton.ToArray();
            Message message = await botClient.SendTextMessageAsync(chatId: _user.id,
                                text: "Если на данный момент есть игровые комнаты, вы увидите их в списке ниже.\nКак только будет появляться новая комната, она будет добавлена в список автоматически.",
                                replyMarkup: inlineKeyboard);
            NewGameRoom = UpdateKeyBoard;
            DeleteGameRoom = UpdateKeyBoard;
            async Task<Message> UpdateKeyBoard()
            {
                return await SendInlineKeyboardJoinGame(botClient, message.MessageId, _user);
            }
            return message;

        }
        private static async Task BotOnKeyboardPushAnswerJoinRoom(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            if (callbackQuery.Data == "/back")
            {
                _user.state = User.states.Main;
                await SendInLineKeybordMain(botClient, callbackQuery.Message!.MessageId, _user);
            }
            else
            {
                var success = long.TryParse(callbackQuery.Data, out long idGameRoom);
                _user.UserIdJoinRoom = idGameRoom;
                foreach (var room in Rooms)
                {
                    if (room.Value == idGameRoom) room.Key.TryAddJoinUser(_user);
                }
                _user.state = User.states.WaitGame;
                await SendInlineKeyboardWaitGame(botClient, callbackQuery.Message!.MessageId, _user);
            }
        }
        private static async Task<Message> SendInlineKeyboardWaitGame(ITelegramBotClient botClient, int messageId, User _user)
        {
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Правила","/rules"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("<= Назад","/back"),
                    }
                }
                );
            return await botClient.EditMessageTextAsync(chatId: _user.id,
                            messageId: messageId,
                            text: $"Вы добавлены в игровую комнату с Id: {_user.UserIdJoinRoom}!\nОжидайте, когда создатель комнаты запустит игру, а пока можете снова прочесть правила.",
                            replyMarkup: inlineKeyboard);
        }
        private static async Task BotOnKeyboardPushAnswerWaitGame(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            switch (callbackQuery.Data)
            {
                case "/rules":
                    _user.state = User.states.Rulles;
                    await SendInLineKeybordRules(botClient, callbackQuery.Message!.MessageId, _user, "/backwait");
                    break;
                case "/back":
                    _user.state = User.states.GameJoin;
                    await SendInlineKeyboardJoinGame(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
            }
        }
        private static async Task<Message> SendInLineKeybordGameHost(ITelegramBotClient botClient, int messageId, User _user)
        {
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                    {
                     new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Посмотреть подключенных игроков","/showplayers"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Начать игру!","/startgame"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Правила","/rules"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("<= Закрыть комнату","/exit"),
                        }
                    }
                    );
            return await botClient.EditMessageTextAsync(chatId: _user.id,
                            messageId: messageId,
                            text: $"Вы создали игровую комнату! Id комнаты - {_user.UserHostRoom.id}!\n" +
                            "Когда в комнату будут заходить новые игроки, Это будет отображаться в соответствующей кнопке.\n" +
                            "Когда Вы будете готовы начать, нажмите на соответствующую кнопку:",
                            replyMarkup: inlineKeyboard);
        }
        private static async Task BotOnKeyboardPushAnswerGameHost(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            switch (callbackQuery.Data)
            {
                case "/showplayers":
                    await ShowJoinPlayersAsync(botClient, callbackQuery, _user);
                    break;
                case "/startgame":
                    await StartGameAsync(botClient, _user);
                    break;
                case "/exit":
                    await ExitHostRoom(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
                case "/rules":
                    _user.state = User.states.Rulles;
                    await SendInLineKeybordRules(botClient, callbackQuery.Message!.MessageId, _user, "/backhost");
                    break;
            }
        }
        private static async Task<Message> SendInLineKeybordRules(ITelegramBotClient botClient, int messageId, User _user, string back)
        {
            var rules = "Игра Наборщик - соревновательная многопользовательская игра. После команды к началу игры, Вы получите Ключевое слово(КС)." +
                " С этого момента начнется отсчет времени: у Вас будет ровно 3 минуты, чтобы составить из букв Ключевого слова другие слова." +
                " Эти слова должны состоять только из букв КС(с учетом количества этих букв в КС). Принимаются только существительные в единственном числе," +
                " не являющиеся именами собственными. Все Ваши слова Вы должны отправлять мне отдельными сообщениями." +
                "\nВаша задача - Составить как можно больше слов!" +
                "\nP.S.: При подсчете очков все слова, которые совпадают у нескольких соперников, вычеркиваются у обоих. Победителем будет " +
                "считаться тот, кто составит наибольшее количество уникальных слов.";
            await Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                    {
                     new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Понятно",back)
                        }
                    }
                    );
            return await botClient.EditMessageTextAsync(chatId: _user.id,
                            messageId: messageId,
                            text: rules,
                            replyMarkup: inlineKeyboard);
        }
        private static async Task BotOnKeyboardPushAnswerRulles(ITelegramBotClient botClient, CallbackQuery? callbackQuery, User _user)
        {
            switch (callbackQuery.Data)
            {
                case "/backmain":
                    _user.state = User.states.Main;
                    await SendInLineKeybordMain(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
                case "/backwait":
                    _user.state = User.states.WaitGame;
                    await SendInlineKeyboardWaitGame(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
                case "/backhost":
                    _user.state = User.states.GameHost;
                    await SendInLineKeybordGameHost(botClient, callbackQuery.Message!.MessageId, _user);
                    break;
            }
        }
        private static async Task BotOnMessageReceivedGame(ITelegramBotClient botClient, Message message, User _user)
        {
            var action = message.Text?.ToLower().Trim(' '); ;
            if (!_user._words.Contains(action))
            {
                _user._words.Add(action);

            }

        }
        private static async Task StartTrainingAsync(ITelegramBotClient botClient, User _user)
        {
            var trainingRoom = new TrainingRoom(_user);
            _user.state = User.states.Game;
            await botClient.SendTextMessageAsync(chatId: _user.id, text: $"Ваше Ключевое слово: {trainingRoom._dBRussiunWords.GetKeyWord(8, 15)}");
            _user.UserTimer = new System.Timers.Timer(180000);
            _user.UserTimer.Elapsed += _user.TimeIsOver;
            _user.UserTimer.Start();
        }
        private static async Task ShowJoinPlayersAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, User _user)
        {
            string ListPlayers = "";
            var joinUsersNow = _user.UserHostRoom.JoinUsers.Keys;

            if (joinUsersNow.Count() == 0)
                ListPlayers = "Пока что вы тут одни";
            else if (joinUsersNow.Count() > 6)
                ListPlayers = "В комнате больше 6 подключенных пользователей";
            else
                foreach (var player in joinUsersNow)
                    ListPlayers += player.Name + ": " + player.id.ToString() + "\n";

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, ListPlayers, true);

            await Task.Delay(300);
            try
            {
                InlineKeyboardMarkup inlineKeyboard = new(
                 new[]
                    {
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Посмотреть подключенных игроков","/showplayers"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Начать игру!","/startgame"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Правила","/rules"),
                        },
                    new[]
                        {
                            InlineKeyboardButton.WithCallbackData("<= Закрыть комнату","/exit"),
                        }
                    }
                     );
                await botClient.EditMessageTextAsync(chatId: _user.id,
                                messageId: callbackQuery.Message!.MessageId,
                                text: $"Вы создали игровую комнату!\n" +
                                "Когда в комнату будут заходить новые игроки, Это будет отображаться в соответствующей кнопке.\n" +
                                "Когда Вы будете готовы начать, нажмите на соответствующую кнопку:",
                                replyMarkup: inlineKeyboard);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException) { Console.WriteLine("Клавиатура не обновлена, так как нечего изменять"); }

        }
        private static async Task CreateGameAsync(ITelegramBotClient botClient, int messageId, User _user)
        {
            GameRoom gameRoom = new GameRoom(_user);
            var success = Rooms.TryAdd(gameRoom, gameRoom.id);
            if (success) NewGameRoom?.Invoke();
            gameRoom.HostKeyboardId = messageId;
            await SendInLineKeybordGameHost(botClient, messageId, _user);
        }
        private static async Task StartGameAsync(ITelegramBotClient botClient, User _user)
        {
            var gameRoom = _user.UserHostRoom;
            gameRoom.KeyWord = gameRoom._dBRussiunWords.GetKeyWord(8, 15);
            gameRoom.JoinUsers.TryAdd(_user, _user.id);
            foreach (var joinUser in gameRoom.JoinUsers)
            {
                await botClient.SendTextMessageAsync(chatId: joinUser.Key.id, text: $"Старт дан! Итак, ваше ключевое слово...");
            }
            await Task.Delay(500);
            foreach (var joinUser in gameRoom.JoinUsers)
            {
                joinUser.Key.state = User.states.Game;
                await botClient.SendTextMessageAsync(chatId: joinUser.Key.id, text: gameRoom.KeyWord);
            }
            gameRoom.GameTimer = new System.Timers.Timer(180000);
            gameRoom.GameTimer.Elapsed += gameRoom.TimeIsOver;
            gameRoom.GameTimer.Start();
        }

        private static async Task ExitHostRoom(ITelegramBotClient botClient, int messageId, User _user)
        {
            _user.state = User.states.Main;
            var success = Rooms.TryRemove(_user.UserHostRoom, out long value);
            foreach (var user in _user.UserHostRoom.JoinUsers)
            {
                await botClient.SendTextMessageAsync(chatId: user.Value, text: "Создатель удалил эту комнату, вы вернулись в меню выбора комнаты.");
                user.Key.state = User.states.GameJoin;
                await SendInlineKeyboardJoinGame(botClient, user.Key);
            }
            _user.UserHostRoom = null;
            await SendInLineKeybordMain(botClient, messageId, _user);
            if (success) DeleteGameRoom?.Invoke();
        }

        private static async Task StartMessageAsync(ITelegramBotClient botClient, User _user)
        {
            var userName = _user.Name;
            var startInstruction = "\nЯ ведущий игры \"Наборщик\"!\nНадеюсь, Вам понравится время, проведенное со мной. \nПредлагаю ознакомиться с правилами, либо начать игру, если их уже знаете!";
            await botClient.SendTextMessageAsync(chatId: _user.id, text: $"Добро пожаловать {userName}!{startInstruction}");
        }


        static async Task Main()
        {
            string botToken;
                using (StreamReader reader = new StreamReader("token.txt"))
                {
                    botToken = reader.ReadLine();
                }
            var bot = new TelegramBotClient(botToken);
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, 
            };

            bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken);

            Console.ReadLine();
        }


    }
}