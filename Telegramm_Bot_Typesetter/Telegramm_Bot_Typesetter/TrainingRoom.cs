using System.Collections.Concurrent;

namespace Telegramm_Bot_Typesetter
{
    public class TrainingRoom
    {
        public DBRussiunWords _dBRussiunWords = new DBRussiunWords();
        public List<string> ReceivedWords = new List<string>();
        public int CountCorrectWords = 0;
        public int CountUncorrectWords = 0;
        public int CountReceivedWords => ReceivedWords.Count;
        public void GetWordsFromTraining(List<string> resivedWords)
        {
            foreach (var word in resivedWords)
            {
                ReceivedWords.Add(word);
                if (_dBRussiunWords.IsRealWord(word)) CountCorrectWords++;
                else CountUncorrectWords++;
            }
        }
        public TrainingRoom(User _user)
        {
            _user.UserTrainingRoom = this;
        }
        
    }
}
