using Dapper;
using Npgsql;
namespace Telegramm_Bot_Typesetter
{
    public class DBRussiunWords
    {
    
        public static string SqlConnectionString = "User ID=postgres;Password=123;Host=localhost;Port=5432;Database=RussianNouns;";
        public class Nouns
        {
            public string word;
        }
        public bool IsRealWord(string word)
        {
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var query = $"select word from nouns where word = '{word}'";
                var list = connection.Query<Nouns>(query).ToList();
                if (list.Count>0) return true;
                else return false;
            }
        }
        public string GetKeyWord(int min, int max)
        {
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var query = $"select word from nouns where char_length(word) >{min} and char_length(word) <{max} order by random() LIMIT 1";
                var list = connection.Query<Nouns>(query);
                var WordOrNull = list.SingleOrDefault();
                if (WordOrNull != null) return WordOrNull.word;
                else throw new ArgumentNullException(); 
            }
        }
        
    }
}

