using System.Collections.Concurrent;
using System.Text;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static Telegramm_Bot_Typesetter.User;

namespace Telegramm_Bot_Typesetter
{
    public class GameRoom : TrainingRoom
    {
        public System.Timers.Timer GameTimer;
        public event Func<Task> NewJoinUser;
        public event Func<Task> UserLeftTheRoom;
        public long id;
        public ConcurrentDictionary<User, long> JoinUsers = new ConcurrentDictionary<User, long>();
        public User UserHost;
        public int HostKeyboardId;
        public string KeyWord;
        public string Name;
        public GameRoom(User _user) : base(_user)
        {
            UserHost = _user;
            _user.UserHostRoom = this;
            id = _user.id + 111111111;
            NewJoinUser += UpdateListJoinUsers;
            UserLeftTheRoom += UpdateListJoinUsers;
        }

        public async Task<Message> UpdateListJoinUsers()
        {
            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                    {
                     new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Посмотреть подключенных игроков (Есть изменения!!!)","/showplayers"),
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
            return await UserHost._botClient.EditMessageTextAsync(chatId: UserHost.id,
                                  messageId: HostKeyboardId,
                                  text: $"Вы создали игровую комнату!\n" +
                            "Когда в комнату будут заходить новые игроки, Это будет отображаться в соответствующей кнопке.\n" +
                            "Когда Вы будете готовы начать, нажмите на соответствующую кнопку:",
                                  replyMarkup: inlineKeyboard);
        }
        public bool TryAddJoinUser(User _user)
        {
            var success = JoinUsers.TryAdd(_user, _user.id);
            if (success) NewJoinUser?.Invoke();
            return success;
        }
        public bool TryRemoveJoinUser(User _user)
        {
            var success = JoinUsers.TryRemove(_user, out long value);
            if (success) UserLeftTheRoom?.Invoke();
            return success;
        }

        public void TimeIsOver(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"Время вышло, игра в комнате с id {id} закончена");
            GameTimer.Stop();
            GameTimer.Close();
            foreach (var user in JoinUsers.Keys)
            {
                if (user.Equals(UserHost)) user.state = states.GameHost;
                else user.state = states.WaitGame;
                user.CountReciveWords = user._words.Count;
                WordsSpellCheck(ref user._words);
                FilterRealWordsDB(ref user._words);
                user.CountCorrectWords = user._words.Count;
            }
            DeleteMatch();
            foreach (var user in JoinUsers.Keys) user.CountOriginalWords = user._words.Count;
            foreach (var user in JoinUsers.Keys)
            {
                user._botClient.SendTextMessageAsync(chatId: user.id, text: GetResult(user));
                if (user.Equals(UserHost)) SendInLineKeybordGameHost(user);
                else SendInLineKeybordWaitGame(user);
            }
            ClearAllData();
        }
        private Task<Message> SendInLineKeybordGameHost(User _user)
        {
            Task.Delay(300);
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
            return _user._botClient.SendTextMessageAsync(chatId: _user.id, text: $"Игра завершена!\nВы перемещены в меню игровой комнаты.",
                            replyMarkup: inlineKeyboard);
        }
        private Task<Message> SendInLineKeybordWaitGame(User _user)
        {
            Task.Delay(300);
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

            return _user._botClient.SendTextMessageAsync(chatId: _user.id, text: $"Игра завершена!\nВы перемещены в меню игровой комнаты.",
                            replyMarkup: inlineKeyboard);
        }
        private void ClearAllData()
        {

            foreach (var user in JoinUsers.Keys)
            {
                user._words.Clear();
                user.CountReciveWords = 0;
                user.CountCorrectWords = 0;
                user.CountOriginalWords = 0;
            }
            JoinUsers.TryRemove(UserHost, out long value);

        }

        private string GetResult(User _user)
        {

            string result = $"Время вышло!\nВы успели составить {_user.CountReciveWords} сл.: отличный результат!\nИз них корректрых слов: {_user.CountCorrectWords};\n" +
                            $"После удаления совпадающих с другими игроками слов у вас осталось {_user.CountOriginalWords}.\n\n";
            Dictionary<string, int> Places = new Dictionary<string, int>();
            foreach (var user in JoinUsers) Places.Add(user.Key.Name, user.Key.CountOriginalWords);
            Places = Places.OrderByDescending(х => х.Value).ToDictionary(x => x.Key, x => x.Value);
            int PlaceCounter = 1;
            StringBuilder places = new StringBuilder($"А вот и расстановка на пъедестале почета:\n{PlaceCounter}-е место : {Places.ElementAt(0).Key} - {Places.ElementAt(0).Value} сл.\n");
            for (int i = 1; i < Places.Count; i++)
            {
                if (Places.ElementAt(i).Value == Places.ElementAt(i - 1).Value) places.Append($"{PlaceCounter}-е место : {Places.ElementAt(i).Key} - {Places.ElementAt(i).Value} сл.\n");
                else places.Append($"{++PlaceCounter}-е место : {Places.ElementAt(i).Key} - {Places.ElementAt(i).Value} сл.\n");
            }
            return result + places.ToString();
        }

        private void DeleteMatch()
        {
            var NonUniqueWords = new List<string>();
            for (int i = 0; i < JoinUsers.Count; i++)
            {
                for (int j = i + 1; j < JoinUsers.Count; j++)
                {
                    foreach (var wordA in JoinUsers.ElementAt(i).Key._words)
                    {
                        foreach (var wordB in JoinUsers.ElementAt(j).Key._words)
                        {
                            if (wordA.Equals(wordB))
                            {
                                NonUniqueWords.Add(wordA);
                                break;
                            }
                        }
                    }
                }
            }
            foreach (var word in NonUniqueWords)
            {
                foreach (var user in JoinUsers.Keys)
                {
                    user._words.Remove(word);
                }
            }
        }
        private void WordsSpellCheck(ref List<string> words)
        {
            var filtredWords = new List<string>();
            foreach (var word in words)
            {
                bool wordIsVerified = true;
                foreach (var letter in word)
                {
                    if (CountCharInWord(word, letter) > CountCharInWord(KeyWord, letter))
                    {
                        wordIsVerified = false;
                        break;
                    }
                }
                if (wordIsVerified) filtredWords.Add(word);
            }
            words = filtredWords;
        }

        private byte CountCharInWord(string word, char letter)
        {
            byte countLetter = 0;
            foreach (var wordChar in word)
            {
                if (wordChar == letter) countLetter++;
            }
            return countLetter;
        }
        private void FilterRealWordsDB(ref List<string> words)
        {
            var filtredWords = new List<string>();
            foreach (var word in words)
            {
                if (_dBRussiunWords.IsRealWord(word)) filtredWords.Add(word);
            }
            words = filtredWords;
        }
    }
}