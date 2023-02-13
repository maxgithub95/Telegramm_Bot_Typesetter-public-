using Telegram.Bot;
using System.Collections.Concurrent;
using System.Timers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace Telegramm_Bot_Typesetter
{
    public class User
    {
        public enum states
        {
            Rulles,
            Main,
            TrainingMenu,
            Training,
            GameHost,
            GameJoin,
            WaitGame,
            Game
        }
        public string Name { get; set; }
        public long id { get; set; }  
        public states state { get; set; }
        public ITelegramBotClient _botClient;
        public User(long id, ITelegramBotClient botClient, string name)
        {
            _botClient = botClient;
            this.id = id;
            state = states.Main;
            Name= name;
        }

        public System.Timers.Timer UserTimer;
        public TrainingRoom UserTrainingRoom;
        public List<string> _words= new List<string>();
        public GameRoom UserHostRoom; //если пользователь Host, то это поле заполняется ссылкой на созданную им игровую комнату (Create)
        public long UserIdJoinRoom; //Id игровой комнаты, к которой подключился игрок. (Join)
        public int CountReciveWords;
        public int CountCorrectWords;
        public int CountOriginalWords;

        public void TimeIsOver(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine("User прошел тренировку");
            state = states.TrainingMenu;
            UserTrainingRoom.GetWordsFromTraining(_words);
            _words.Clear();            
            _botClient.SendTextMessageAsync(chatId: id, text: GetResult());
            SendInLineKeybordTrainingMenu();
            UserTimer.Stop();            
            UserTimer.Close();            
        }
        private Task<Message> SendInLineKeybordTrainingMenu()
        {
            Task.Delay(300);
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Пройти тренировку еще раз","/starttraining"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("<= Выйти в главное меню","/back"),
                    }
                }
                );
            return _botClient.SendTextMessageAsync(chatId: id,
                            text: "Вы находитесь в меню тренировки:",
                            replyMarkup: inlineKeyboard);
        }
        public string GetResult()
        {
            string result = $"Время вышло!\nВы успели составить {UserTrainingRoom.CountReceivedWords} сл.!\nИз них корректрых слов: {UserTrainingRoom.CountCorrectWords};\n" +
                $"Ваш результат тренировки {UserTrainingRoom.CountCorrectWords}!";
            return result;
        }
    }
    
}
