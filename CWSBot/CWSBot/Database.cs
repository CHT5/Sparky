using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CWSBot
{
    public class Database : ModuleBase
    {
        #region Basic Database initiation stuff


        private string table { get; set; }
        private const string server = "localhost";
        private const string database = "liteboxdb";
        private const string username = "root";
        private const string password = "Potatotzr4ever!";
        private MySqlConnection dbConnection;


        //REMEMBER TO ALWAYS CLOSE A CONNECTION
        public Database(string table)
        {
            this.table = table;
            MySqlConnectionStringBuilder stringBuilder = new MySqlConnectionStringBuilder();
            stringBuilder.Server = server;
            stringBuilder.UserID = username;
            stringBuilder.Password = password;
            stringBuilder.Database = database;
            stringBuilder.SslMode = MySqlSslMode.None;

            var connectionString = stringBuilder.ToString();

            dbConnection = new MySqlConnection(connectionString);

            dbConnection.Open();
        }

        //FIRE ANY MYSQL COMMANDS ENTERED!
        public MySqlDataReader FireCommand(string query)
        {
            if (dbConnection == null)
            {
                return null;
            }

            MySqlCommand command = new MySqlCommand(query, dbConnection);

            var mySqlReader = command.ExecuteReader();

            return mySqlReader;
        }

        #endregion

        #region Basic User init related methods

        //CHECK IF THE USER IS IN THE DATABASE, THEN RETURN IT AS PART OF A STRING LIST
        public static List<String> CheckExistingUser(IUser user)
        {
            var result = new List<String>();
            var database = new Database("liteboxdb");
            var str = string.Format($"SELECT * FROM liteboxtb_users WHERE user_id = '{user.Id}'");
            var tokens = database.FireCommand(str);
            while (tokens.Read())
            {
                var userId = (string)tokens["UserID"];

                result.Add(userId);
            }
            return result;
        }

        //ENTERS A NEW USER
        public static void EnterUser(IUser user, int register_Age)  // <--- THE CONSTRUCTOR
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"INSERT INTO liteboxtb_users (user_id, username, age) VALUES ('{user.Id}', '{user.Username}', '{register_Age}')"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }

        //REMOVES A USER
        public static void RemoveUser(IUser user)  // <--- THE CONSTRUCTOR
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"DELETE FROM liteboxtb_users WHERE user_id = '{user.Id}'"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }

        //GRABS A USERS DATA
        public static List<CWSBottb_users> GetUserStatus(IUser user)
        {
            var result = new List<CWSBottb_users>();

            var database = new Database("liteboxdb");

            var str = string.Format($"SELECT * FROM liteboxtb_users WHERE user_id = '{user.Id}'");
            var data = database.FireCommand(str);

            while (data.Read())
            {
                var User_Id = (string)data["user_id"];
                var Age = (int)data["age"];
                var LiteTokens = (int)data["litetokens"];
                var Karma = (int)data["karma"];
                var WarningCount = (int)data["warningcount"];
                var KarmaParticipation = (DateTime)data["karmaparticipation"];

                result.Add(new CWSBottb_users
                {
                    user_id = User_Id,
                    age = Age,
                    liteTokens = LiteTokens,
                    karma = Karma,
                    warningCount = WarningCount,
                    karmaParticipation = KarmaParticipation
                });
            }
            database.CloseConnection();

            return result;

        }
        #endregion

        #region Mod Related Stuff

        public static void WarnUser(IUser user, int karmaReduction)  // <--- THE CONSTRUCTOR
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"UPDATE liteboxtb_users SET karma = karma - {karmaReduction}, warningcount = warningcount + 1 WHERE user_id = '{user.Id}'"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }


        #endregion

        #region Basic User Commands

        public static void RepUser(IUser user, int warningCounter)  // <--- THE CONSTRUCTOR
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"UPDATE liteboxtb_users SET litetokens = litetokens +1, karma = karma + 1, warningcount = warningcount {warningCounter}, karmaparticipation = now() WHERE user_id = '{user.Id}'"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }

        public static void RepDateChange(IUser user)
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"UPDATE liteboxtb_users SET karmaparticipation = now() WHERE user_id = '{user.Id}'"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }

        public static void TokenChange(IUser user, int amount)
        {
            var database = new Database("liteboxdb");
            var str = string.Format($"UPDATE liteboxtb_users SET litetokens = litetokens + {amount} WHERE user_id = '{user.Id}'"); // <--- THE STRING YOU'LL HAVE TO EDIT
            var tokens = database.FireCommand(str);
            database.CloseConnection();

            return;
        }

        #endregion


        public void CloseConnection()
        {
            if (dbConnection != null)
            {
                dbConnection.Close();
            }
        }

    }
}
