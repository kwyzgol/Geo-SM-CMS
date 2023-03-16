namespace WebApp.Cms;

public class DatabasesManager
{
    private readonly string _mysqlHostname;
    private readonly string _mysqlDatabase;
    private readonly string _mysqlUser;
    private readonly string _mysqlPassword;
    private readonly string _neo4jHostname;
    private readonly string _neo4jUser;
    private readonly string _neo4jPassword;

    public DatabasesManager(string mysqlHostname, string mysqlDatabase, string mysqlUser, string mysqlPassword, string neo4jHostname, string neo4jUser, string neo4jPassword)
    {
        _mysqlHostname = mysqlHostname;
        _mysqlDatabase = mysqlDatabase;
        _mysqlUser = mysqlUser;
        _mysqlPassword = mysqlPassword;
        _neo4jHostname = neo4jHostname;
        _neo4jUser = neo4jUser;
        _neo4jPassword = neo4jPassword;
    }

    // CORE

    private async Task CloseConnection(IAsyncSession? neo4jSession = null, IDriver? neo4jDriver = null, MySqlConnection? mySqlConnection = null)
    {
        try
        {
            if (neo4jSession != null) await neo4jSession.CloseAsync();
            if (neo4jDriver != null) await neo4jDriver.CloseAsync();
            mySqlConnection?.Close();
        }
        catch (Exception exc)
        {
            Console.WriteLine(exc.Message);
        }
    }

    public void SetAsGlobal()
    {
        DatabaseConnected.DatabasesBase = this;
    }

    public async Task<OperationResult> SampleRequest()
    {
        // START
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                // REQUESTS

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
        // END
    }

    public async Task<OperationResult> TestConnection()
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            bool mysqlResult = false;
            string cmdString = "SELECT 'test'";
            var cmd = new MySqlCommand(cmdString, mySqlConnection);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string cmdResult = reader.GetString(0);
                mysqlResult = cmdResult.Equals("test");
                reader.Close();
            }

            bool neo4jResult = false;
            var neo4jCmd = await neo4jSession.RunAsync("RETURN 'test'");
            var neo4jCmdResult = await neo4jCmd.SingleAsync();
            if (neo4jCmdResult[0].As<string>().Equals("test")) neo4jResult = true;

            await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
            return new OperationResult(mysqlResult && neo4jResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckTableExist()
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            string cmdString = "SHOW TABLES LIKE 'settings'";
            MySqlCommand cmd = new MySqlCommand(cmdString, mySqlConnection);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult MySqlInit()
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            try
            {
                string cmdEvents =
                    "CREATE EVENT delete_expired_auth_codes " +
                    "ON SCHEDULE EVERY 1 MINUTE " +
                    "STARTS '1970-01-01 00:00:01' " +
                    "ON COMPLETION PRESERVE ENABLE DO " +
                    "DELETE FROM auth_codes WHERE CURRENT_TIMESTAMP() > valid_time;;" +
                    "" +
                    "" +
                    "" +
                    "CREATE EVENT delete_expired_ban_events " +
                    "ON SCHEDULE EVERY 1 MINUTE " +
                    "STARTS '1970-01-01 00:00:01' " +
                    "ON COMPLETION PRESERVE ENABLE DO " +
                    "BEGIN " +
                    "DELETE FROM events " +
                    "WHERE type = 'ban' AND CURRENT_TIMESTAMP() > valid_time; " +
                    "UPDATE users SET status = 'active' " +
                    "WHERE status = 'banned' AND user_id NOT IN " +
                        "(SELECT DISTINCT user_id FROM events WHERE type = 'ban'); " +
                    "END;; " +
                    "" +
                    "" +
                    "" +
                    "CREATE EVENT delete_not_activated_users " +
                    "ON SCHEDULE EVERY 15 MINUTE " +
                    "STARTS '1970-01-01 00:00:01' " +
                    "ON COMPLETION PRESERVE ENABLE DO " +
                    "BEGIN " +
                    "DELETE FROM users " +
                    "WHERE status = 'registered' " +
                    "AND user_id IN " +
                        "(SELECT user_id FROM events " +
                        "WHERE type = 'registration' " +
                        "AND CURRENT_TIMESTAMP() > valid_time); " +
                    "DELETE FROM events " +
                    "WHERE type = 'registration' " +
                    "AND CURRENT_TIMESTAMP() > valid_time; " +
                    "END;; " +
                    "" +
                    "" +
                    "" +
                    "CREATE EVENT unlock_report " +
                    "ON SCHEDULE EVERY 15 MINUTE " +
                    "STARTS '1970-01-01 00:00:01' " +
                    "ON COMPLETION PRESERVE ENABLE DO " +
                    "BEGIN " +
                    "UPDATE reports SET moderator_id = NULL, status = 'active' " +
                    "WHERE report_id IN " +
                        "(SELECT report_id FROM events " +
                        "WHERE type = 'locked report' " +
                        "AND CURRENT_TIMESTAMP() > valid_time); " +
                    "DELETE FROM events " +
                    "WHERE type = 'locked report' " +
                    "AND CURRENT_TIMESTAMP() > valid_time; " +
                    "END;;";

                string cmdTables =
                    "DROP TABLE IF EXISTS roles; " +
                    "CREATE TABLE roles " +
                    "(role_id int unsigned NOT NULL, " +
                    "role_name varchar(50) NOT NULL, " +
                    "PRIMARY KEY (role_id)) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "INSERT INTO roles (role_id, role_name) VALUES " +
                    "(0,'admin'), " +
                    "(1, 'moderator'), " +
                    "(2, 'fact checker'), " +
                    "(3, 'user'); " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS users; " +
                    "CREATE TABLE users " +
                    "(user_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "username varchar(20) NOT NULL, " +
                    "password varchar(100) NOT NULL, " +
                    "email varchar(320) DEFAULT NULL, " +
                    "phone_country varchar(4) DEFAULT NULL, " +
                    "phone_number varchar(15) DEFAULT NULL, " +
                    "status varchar(40) NOT NULL DEFAULT 'registered', " +
                    "role_id int unsigned NOT NULL DEFAULT 3, " +
                    "PRIMARY KEY (user_id), " +
                    "UNIQUE KEY unique_username (username), " +
                    "KEY role_id (role_id), " +
                    "CONSTRAINT users_fk_1 " +
                        "FOREIGN KEY (role_id) " +
                        "REFERENCES roles (role_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci;" +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS login_history; " +
                    "CREATE TABLE login_history " +
                    "(record_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "login_datetime datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "PRIMARY KEY (record_id), " +
                    "KEY user_id (user_id), " +
                    "CONSTRAINT login_history_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS access_tokens; " +
                    "CREATE TABLE access_tokens " +
                    "(token_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "value varchar(100) NOT NULL, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "login_record_id bigint unsigned NOT NULL, " +
                    "PRIMARY KEY (token_id), " +
                    "KEY user_id (user_id), " +
                    "KEY login_record_id (login_record_id), " +
                    "CONSTRAINT access_tokens_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT access_tokens_fk_2 " +
                        "FOREIGN KEY (login_record_id) " +
                        "REFERENCES login_history (record_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS auth_codes; " +
                    "CREATE TABLE auth_codes " +
                    "(code_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "value int unsigned NOT NULL, " +
                    "valid_time datetime NOT NULL, " +
                    "type varchar(20) NOT NULL, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "PRIMARY KEY (code_id), " +
                    "KEY user_id (user_id), " +
                    "CONSTRAINT auth_codes_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS ban_history; " +
                    "CREATE TABLE ban_history " +
                    "(ban_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "reason varchar(500) NOT NULL, " +
                    "date_start datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
                    "date_end datetime NOT NULL, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "moderator_id bigint unsigned DEFAULT NULL, " +
                    "PRIMARY KEY (ban_id), " +
                    "KEY user_id (user_id), " +
                    "KEY moderator_id (moderator_id), " +
                    "CONSTRAINT ban_history_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT ban_history_fk_2 " +
                        "FOREIGN KEY (moderator_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE SET NULL) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS comments; " +
                    "CREATE TABLE comments " +
                    "(comment_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "PRIMARY KEY (comment_id), " +
                    "KEY user_id (user_id), " +
                    "CONSTRAINT comments_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS messages; " +
                    "CREATE TABLE messages " +
                    "(message_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "sender_id bigint unsigned NOT NULL, " +
                    "receiver_id bigint unsigned NOT NULL, " +
                    "content varchar(1000) NOT NULL, " +
                    "PRIMARY KEY (message_id), " +
                    "KEY sender_id (sender_id), " +
                    "KEY receiver_id (receiver_id), " +
                    "CONSTRAINT messages_fk_1 " +
                        "FOREIGN KEY (sender_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT messages_fk_2 " +
                        "FOREIGN KEY (receiver_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB DEFAULT " +
                    "CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS posts; " +
                    "CREATE TABLE posts " +
                    "(post_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "PRIMARY KEY (post_id), " +
                    "KEY user_id (user_id), " +
                    "CONSTRAINT posts_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS reports; " +
                    "CREATE TABLE reports " +
                    "(report_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "status varchar(50) NOT NULL DEFAULT 'active', " +
                    "type varchar(50) NOT NULL, " +
                    "content varchar(5000) NOT NULL, " +
                    "moderator_id bigint unsigned DEFAULT NULL, " +
                    "report_creator bigint unsigned DEFAULT NULL, " +
                    "reported_post_id bigint unsigned DEFAULT NULL, " +
                    "reported_comment_id bigint unsigned DEFAULT NULL, " +
                    "reported_message_id bigint unsigned DEFAULT NULL, " +
                    "PRIMARY KEY (report_id), " +
                    "KEY report_creator (report_creator), " +
                    "KEY reported_post_id (reported_post_id), " +
                    "KEY reported_comment_id (reported_comment_id), " +
                    "KEY reported_message_id (reported_message_id), " +
                    "KEY moderator_id (moderator_id), " +
                    "CONSTRAINT reports_fk_2 " +
                        "FOREIGN KEY (report_creator) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE SET NULL, " +
                    "CONSTRAINT reports_fk_3 " +
                        "FOREIGN KEY (reported_post_id) " +
                        "REFERENCES posts (post_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT reports_fk_4 " +
                        "FOREIGN KEY (reported_comment_id) " +
                        "REFERENCES comments (comment_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT reports_fk_5 " +
                        "FOREIGN KEY (reported_message_id) " +
                        "REFERENCES messages (message_id) " +
                        "ON DELETE CASCADE ON UPDATE RESTRICT, " +
                    "CONSTRAINT reports_fk_6 " +
                        "FOREIGN KEY (moderator_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE SET NULL ON UPDATE RESTRICT) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS events; " +
                    "CREATE TABLE events " +
                    "(event_id bigint unsigned NOT NULL AUTO_INCREMENT, " +
                    "type varchar(50) NOT NULL, " +
                    "valid_time datetime NOT NULL, " +
                    "user_id bigint unsigned NOT NULL, " +
                    "ban_id bigint unsigned DEFAULT NULL, " +
                    "report_id bigint unsigned DEFAULT NULL, " +
                    "PRIMARY KEY (event_id), " +
                    "KEY user_id (user_id), " +
                    "KEY ban_id (ban_id), " +
                    "KEY type (type), " +
                    "KEY report_id (report_id), " +
                    "CONSTRAINT events_fk_1 " +
                        "FOREIGN KEY (user_id) " +
                        "REFERENCES users (user_id) " +
                        "ON DELETE CASCADE, " +
                    "CONSTRAINT events_fk_2 " +
                        "FOREIGN KEY (ban_id) " +
                        "REFERENCES ban_history (ban_id) " +
                        "ON DELETE SET NULL ON UPDATE RESTRICT, " +
                    "CONSTRAINT events_fk_3 " +
                        "FOREIGN KEY (report_id) " +
                        "REFERENCES reports (report_id) " +
                        "ON DELETE CASCADE) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS large_data; " +
                    "CREATE TABLE large_data (data_id varchar(50) NOT NULL, " +
                    "value longtext NOT NULL, " +
                    "PRIMARY KEY (data_id)) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS phone_allowlist; " +
                    "CREATE TABLE phone_allowlist " +
                    "(country_code varchar(5) NOT NULL, " +
                    "max_numbers int NOT NULL, " +
                    "PRIMARY KEY (country_code,max_numbers)) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; " +
                    "" +
                    "" +
                    "" +
                    "DROP TABLE IF EXISTS settings; " +
                    "CREATE TABLE settings " +
                    "(setting_id varchar(50) NOT NULL, " +
                    "value varchar(100) NOT NULL, " +
                    "PRIMARY KEY (setting_id)) " +
                    "ENGINE=InnoDB " +
                    "DEFAULT CHARSET=utf8mb4 " +
                    "COLLATE=utf8mb4_0900_ai_ci; ";

                MySqlScript script = new MySqlScript(mySqlConnection, cmdEvents);
                script.Delimiter = ";;";
                script.Execute();

                MySqlCommand cmd = new MySqlCommand(cmdTables, mySqlConnection);
                cmd.ExecuteNonQuery();

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection);

                List<string> cmdList = new List<string>();

                cmdList.Add("DROP EVENT IF EXISTS delete_expired_auth_codes;");
                cmdList.Add("DROP EVENT IF EXISTS delete_expired_ban_events;");
                cmdList.Add("DROP EVENT IF EXISTS delete_expired_geo_ban_events;");
                cmdList.Add("DROP EVENT IF EXISTS delete_not_activated_users;");
                cmdList.Add("DROP EVENT IF EXISTS unlock_report;");

                cmdList.Add("DROP TABLE IF EXISTS settings;");
                cmdList.Add("DROP TABLE IF EXISTS phone_allowlist;");
                cmdList.Add("DROP TABLE IF EXISTS large_data;");
                cmdList.Add("DROP TABLE IF EXISTS geo_history;");
                cmdList.Add("DROP TABLE IF EXISTS events;");
                cmdList.Add("DROP TABLE IF EXISTS reports;");
                cmdList.Add("DROP TABLE IF EXISTS posts;");
                cmdList.Add("DROP TABLE IF EXISTS messages;");
                cmdList.Add("DROP TABLE IF EXISTS comments;");
                cmdList.Add("DROP TABLE IF EXISTS ban_history;");
                cmdList.Add("DROP TABLE IF EXISTS auth_codes;");
                cmdList.Add("DROP TABLE IF EXISTS access_tokens;");
                cmdList.Add("DROP TABLE IF EXISTS login_history;");
                cmdList.Add("DROP TABLE IF EXISTS users;");
                cmdList.Add("DROP TABLE IF EXISTS roles;");

                foreach (var cmdString in cmdList)
                {
                    cmd.CommandText = cmdString;
                    cmd.ExecuteNonQuery();
                }

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> Neo4jInit()
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            string queryIndexUsernames = "CREATE TEXT INDEX usernameIndex IF NOT EXISTS FOR (u:User) ON (u.username)";
            string queryIndexPost = "CREATE TEXT INDEX titleIndex IF NOT EXISTS FOR (p:Post) ON (p.title)";
            string queryIndexTag = "CREATE TEXT INDEX tagIndex IF NOT EXISTS FOR (t:Tag) ON (t.name)";

            await neo4jSession.RunAsync(queryIndexUsernames);
            await neo4jSession.RunAsync(queryIndexPost);
            await neo4jSession.RunAsync(queryIndexTag);

            await CloseConnection(neo4jSession, neo4jDriver, null);
            return new OperationResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult MySqlInsert()
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                GeneralSettings settings = new GeneralSettings();

                cmd.CommandText = $"INSERT INTO settings VALUES('system_name', @system_name)";
                cmd.Parameters.AddWithValue("@system_name", settings.SystemName);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('logo', @logo)";
                cmd.Parameters.AddWithValue("@logo", settings.Logo);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO large_data VALUES('tos', @tos)";
                cmd.Parameters.AddWithValue("@tos", settings.TermsOfService);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO large_data VALUES('contact', @contact)";
                cmd.Parameters.AddWithValue("@contact", settings.Contact);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('auto_report', @auto_report)";
                cmd.Parameters.AddWithValue("@auto_report", settings.AutoReport.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('auto_report_reputation', @auto_report_reputation)";
                cmd.Parameters.AddWithValue("@auto_report_reputation", settings.AutoReportReputation.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('reputation_unlisted', @reputation_unlisted)";
                cmd.Parameters.AddWithValue("@reputation_unlisted", settings.ReputationUnlisted.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('starting_reputation', @starting_reputation)";
                cmd.Parameters.AddWithValue("@starting_reputation", settings.StartingReputation.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('main_color', @main_color)";
                cmd.Parameters.AddWithValue("@main_color", settings.MainColor.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('main_color_font', @main_color_font)";
                cmd.Parameters.AddWithValue("@main_color_font", settings.MainColorFont.ToString());
                cmd.ExecuteNonQuery();

                AuthManager auth = new AuthManager();

                cmd.CommandText = $"INSERT INTO settings VALUES('access_key', @access_key)";
                cmd.Parameters.AddWithValue("@access_key", auth.AccessKey);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('smtp_host', @smtp_host)";
                cmd.Parameters.AddWithValue("@smtp_host", auth.SmtpData.Host);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('smtp_port', @smtp_port)";
                cmd.Parameters.AddWithValue("@smtp_port", auth.SmtpData.Port.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('smtp_user', @smtp_user)";
                cmd.Parameters.AddWithValue("@smtp_user", auth.SmtpData.User);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('smtp_password', @smtp_password)";
                cmd.Parameters.AddWithValue("@smtp_password", auth.SmtpData.Password);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('auth_type', @auth_type)";
                cmd.Parameters.AddWithValue("@auth_type", CmsUtilities.AuthTypeToString(auth.Type));
                cmd.ExecuteNonQuery();

                ReCaptchaManager reCaptcha = new ReCaptchaManager();

                cmd.CommandText = $"INSERT INTO settings VALUES('recaptcha_enabled', @recaptcha_enabled)";
                cmd.Parameters.AddWithValue("@recaptcha_enabled", reCaptcha.ReCaptchaEnabled.ToString());
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('recaptcha_public_key', @recaptcha_public_key)";
                cmd.Parameters.AddWithValue("@recaptcha_public_key", reCaptcha.ReCaptchaPublicKey);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('recaptcha_private_key', @recaptcha_private_key)";
                cmd.Parameters.AddWithValue("@recaptcha_private_key", reCaptcha.ReCaptchaPrivateKey);
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"INSERT INTO settings VALUES('recaptcha_minimum_score', @recaptcha_minimum_score)";
                cmd.Parameters.AddWithValue("@recaptcha_minimum_score", reCaptcha.ReCaptchaMinimumScore.ToString("F", CultureInfo.InvariantCulture));
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> SearchPlace(string place)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            var parameters = new { place = place };

            var neo4jCmd = await neo4jSession.RunAsync("CALL apoc.spatial.geocode($place, 4) " +
                                                       "YIELD latitude, longitude, description", parameters);
            var neo4jCmdResult = await neo4jCmd.ToListAsync();

            var locations = new List<PlaceModel>();

            foreach (var record in neo4jCmdResult)
            {
                var latitude = record["latitude"].As<double>();
                var longitude = record["longitude"].As<double>();
                var description = record["description"].As<string>();

                locations.Add(new PlaceModel(latitude, longitude, description));
            }

            locations = locations
                .GroupBy(x => x.Description)
                .Select(y => y.First())
                .ToList();

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", locations);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> Search(SearchType type, string searchValue, string? accessKey = null)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            bool status = false;
            object? result = null;

            if (type == SearchType.User)
            {
                result = new List<UserModel>();
                var ids = new List<long>();

                // Exact

                var query = "MATCH (u:User) WHERE u.username = $searchValue RETURN u.userId LIMIT 10";
                var parametersExact = new { searchValue = searchValue };
                var neo4jCmd = await neo4jSession.RunAsync(query, parametersExact);
                var neo4jCmdResult = await neo4jCmd.ToListAsync();

                foreach (var record in neo4jCmdResult)
                {
                    var userId = record["u.userId"].As<long>();

                    ids.Add(userId);

                    var user = await GetUser((ulong)userId);

                    if (user is { Status: true, Result: UserModel })
                    {
                        ((List<UserModel>)result).Add((UserModel)(user.Result));
                    }
                }

                // Starts with

                if (((List<UserModel>)result).Count < 10)
                {
                    var limit = 10 - ((List<UserModel>)result).Count;
                    query = "MATCH (u:User) WHERE u.username STARTS WITH $searchValue " +
                            "AND NOT u.userId IN $ids RETURN u.userId LIMIT $limit";
                    var parametersStartsWith = new { searchValue = searchValue, ids = ids, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersStartsWith);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var userId = record["u.userId"].As<long>();

                        ids.Add(userId);

                        var user = await GetUser((ulong)userId);

                        if (user is { Status: true, Result: UserModel })
                        {
                            ((List<UserModel>)result).Add((UserModel)(user.Result));
                        }
                    }
                }

                // Contains

                if (((List<UserModel>)result).Count < 10)
                {
                    var limit = 10 - ((List<UserModel>)result).Count;
                    query = "MATCH (u:User) WHERE u.username CONTAINS $searchValue " +
                            "AND NOT u.userId IN $ids RETURN u.userId LIMIT $limit";
                    var parametersContains = new { searchValue = searchValue, ids = ids, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersContains);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var userId = record["u.userId"].As<long>();

                        ids.Add(userId);

                        var user = await GetUser((ulong)userId);

                        if (user is { Status: true, Result: UserModel })
                        {
                            ((List<UserModel>)result).Add((UserModel)(user.Result));
                        }
                    }
                }

                status = true;
            }
            else if (type == SearchType.Tag)
            {
                result = new List<string>();

                // Exact

                var query = "MATCH (t:Tag)<-[:HAS_TAG]-(:Post) WHERE t.name = $searchValue RETURN DISTINCT t.name LIMIT 10";
                var parametersExact = new { searchValue = searchValue };
                var neo4jCmd = await neo4jSession.RunAsync(query, parametersExact);
                var neo4jCmdResult = await neo4jCmd.ToListAsync();

                foreach (var record in neo4jCmdResult)
                {
                    var tagName = record["t.name"].As<string>();
                    ((List<string>)result).Add(tagName);
                }

                // Starts with

                if (((List<string>)result).Count < 10)
                {
                    var limit = 10 - ((List<string>)result).Count;
                    query = "MATCH (t:Tag)<-[:HAS_TAG]-(:Post) WHERE t.name STARTS WITH $searchValue " +
                            "AND NOT t.name IN $names RETURN DISTINCT t.name LIMIT $limit";
                    var parametersStartsWith = new
                    { searchValue = searchValue, names = (List<string>)result, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersStartsWith);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var tagName = record["t.name"].As<string>();
                        ((List<string>)result).Add(tagName);
                    }
                }

                // Contains

                if (((List<string>)result).Count < 10)
                {
                    var limit = 10 - ((List<string>)result).Count;
                    query = "MATCH (t:Tag)<-[:HAS_TAG]-(:Post) WHERE t.name CONTAINS $searchValue " +
                            "AND NOT t.name IN $names RETURN DISTINCT t.name LIMIT $limit";
                    var parametersContains = new
                    { searchValue = searchValue, names = (List<string>)result, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersContains);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var tagName = record["t.name"].As<string>();
                        ((List<string>)result).Add(tagName);
                    }
                }

                status = true;
            }
            else if (type == SearchType.Post)
            {
                result = new List<PostModel>();
                var ids = new List<long>();

                // Exact

                var query = "MATCH (p:Post) WHERE p.title = $searchValue RETURN p.postId LIMIT 10";
                var parametersExact = new { searchValue = searchValue };
                var neo4jCmd = await neo4jSession.RunAsync(query, parametersExact);
                var neo4jCmdResult = await neo4jCmd.ToListAsync();

                foreach (var record in neo4jCmdResult)
                {
                    var postId = record["p.postId"].As<long>();

                    ids.Add(postId);

                    var post = await GetPost(postId, accessKey);

                    if (post is { Status: true, Result: PostModel })
                    {
                        ((List<PostModel>)result).Add((PostModel)(post.Result));
                    }
                }

                // Starts with

                if (((List<PostModel>)result).Count < 10)
                {
                    var limit = 10 - ((List<PostModel>)result).Count;
                    query = "MATCH (p:Post) WHERE p.title STARTS WITH $searchValue " +
                            "AND NOT p.postId IN $ids RETURN p.postId LIMIT $limit";
                    var parametersStartsWith = new { searchValue = searchValue, ids = ids, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersStartsWith);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var postId = record["p.postId"].As<long>();

                        ids.Add(postId);

                        var post = await GetPost(postId, accessKey);

                        if (post is { Status: true, Result: PostModel })
                        {
                            ((List<PostModel>)result).Add((PostModel)(post.Result));
                        }
                    }
                }

                // Contains

                if (((List<PostModel>)result).Count < 10)
                {
                    var limit = 10 - ((List<PostModel>)result).Count;
                    query = "MATCH (p:Post) WHERE p.title CONTAINS $searchValue " +
                            "AND NOT p.postId IN $ids RETURN p.postId LIMIT $limit";
                    var parametersContains = new { searchValue = searchValue, ids = ids, limit = limit };
                    neo4jCmd = await neo4jSession.RunAsync(query, parametersContains);
                    neo4jCmdResult = await neo4jCmd.ToListAsync();

                    foreach (var record in neo4jCmdResult)
                    {
                        var postId = record["p.postId"].As<long>();

                        ids.Add(postId);

                        var post = await GetPost(postId, accessKey);

                        if (post is { Status: true, Result: PostModel })
                        {
                            ((List<PostModel>)result).Add((PostModel)(post.Result));
                        }
                    }
                }

                status = true;
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(status, "", result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // SETTINGS

    public OperationResult GetSettings(GeneralSettings settings, AuthManager auth, ReCaptchaManager reCaptcha)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'system_name'";
            settings.SystemName = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'logo'";
            settings.Logo = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM large_data WHERE data_id = 'tos'";
            settings.TermsOfService = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM large_data WHERE data_id = 'contact'";
            settings.Contact = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'auto_report'";
            settings.AutoReport = Convert.ToBoolean((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'auto_report_reputation'";
            settings.AutoReportReputation = Convert.ToInt32((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'reputation_unlisted'";
            settings.ReputationUnlisted = Convert.ToInt32((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'starting_reputation'";
            settings.StartingReputation = Convert.ToInt32((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'main_color'";
            settings.MainColor = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'main_color_font'";
            settings.MainColorFont = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'access_key'";
            auth.AccessKey = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'smtp_host'";
            auth.SmtpData.Host = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'smtp_port'";
            auth.SmtpData.Port = Convert.ToInt32((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'smtp_user'";
            auth.SmtpData.User = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'smtp_password'";
            auth.SmtpData.Password = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'auth_type'";
            auth.Type = CmsUtilities.AuthTypeToEnum((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'recaptcha_enabled'";
            reCaptcha.ReCaptchaEnabled = Convert.ToBoolean((string)cmd.ExecuteScalar());

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'recaptcha_public_key'";
            reCaptcha.ReCaptchaPublicKey = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'recaptcha_private_key'";
            reCaptcha.ReCaptchaPrivateKey = (string)cmd.ExecuteScalar();

            cmd.CommandText = "SELECT value FROM settings WHERE setting_id = 'recaptcha_minimum_score'";
            reCaptcha.ReCaptchaMinimumScore = float.Parse((string)cmd.ExecuteScalar(), CultureInfo.InvariantCulture);

            List<PhoneAllowed> phoneAllowed = new List<PhoneAllowed>();
            cmd.CommandText = "SELECT country_code, max_numbers FROM phone_allowlist";
            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string countryCode = reader.GetString(0);
                int maxNumbers = reader.GetInt32(1);
                PhoneAllowed record = new PhoneAllowed(countryCode, maxNumbers);
                phoneAllowed.Add(record);
            }

            auth.PhoneAllowedList = phoneAllowed;

            auth.SetSmtpEmail();

            mySqlConnection.Close();
            return new OperationResult(true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpdateSettings(string accessToken, GeneralSettings settings, AuthManager auth, ReCaptchaManager reCaptcha)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (user.Role != Role.Admin)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                cmd.CommandText = "UPDATE settings SET value = @system_name WHERE setting_id = 'system_name'";
                cmd.Parameters.AddWithValue("@system_name", settings.SystemName);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @logo WHERE setting_id = 'logo'";
                cmd.Parameters.AddWithValue("@logo", settings.Logo);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE large_data SET value = @tos WHERE data_id = 'tos'";
                cmd.Parameters.AddWithValue("@tos", settings.TermsOfService);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE large_data SET value = @contact WHERE data_id = 'contact'";
                cmd.Parameters.AddWithValue("@contact", settings.Contact);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @auto_report WHERE setting_id = 'auto_report'";
                cmd.Parameters.AddWithValue("@auto_report", settings.AutoReport.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @auto_report_reputation WHERE setting_id = 'auto_report_reputation'";
                cmd.Parameters.AddWithValue("@auto_report_reputation", settings.AutoReportReputation.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @reputation_unlisted WHERE setting_id = 'reputation_unlisted'";
                cmd.Parameters.AddWithValue("@reputation_unlisted", settings.ReputationUnlisted.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @starting_reputation WHERE setting_id = 'starting_reputation'";
                cmd.Parameters.AddWithValue("@starting_reputation", settings.StartingReputation.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @main_color WHERE setting_id = 'main_color'";
                cmd.Parameters.AddWithValue("@main_color", settings.MainColor.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @main_color_font WHERE setting_id = 'main_color_font'";
                cmd.Parameters.AddWithValue("@main_color_font", settings.MainColorFont.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @access_key WHERE setting_id = 'access_key'";
                cmd.Parameters.AddWithValue("@access_key", auth.AccessKey);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @smtp_host WHERE setting_id = 'smtp_host'";
                cmd.Parameters.AddWithValue("@smtp_host", auth.SmtpData.Host);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @smtp_port WHERE setting_id = 'smtp_port'";
                cmd.Parameters.AddWithValue("@smtp_port", auth.SmtpData.Port.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @smtp_user WHERE setting_id = 'smtp_user'";
                cmd.Parameters.AddWithValue("@smtp_user", auth.SmtpData.User);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @smtp_password WHERE setting_id = 'smtp_password'";
                cmd.Parameters.AddWithValue("@smtp_password", auth.SmtpData.Password);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @auth_type WHERE setting_id = 'auth_type'";
                cmd.Parameters.AddWithValue("@auth_type", CmsUtilities.AuthTypeToString(auth.Type));
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @recaptcha_enabled WHERE setting_id = 'recaptcha_enabled'";
                cmd.Parameters.AddWithValue("@recaptcha_enabled", reCaptcha.ReCaptchaEnabled.ToString());
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @recaptcha_public_key WHERE setting_id = 'recaptcha_public_key'";
                cmd.Parameters.AddWithValue("@recaptcha_public_key", reCaptcha.ReCaptchaPublicKey);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @recaptcha_private_key WHERE setting_id = 'recaptcha_private_key'";
                cmd.Parameters.AddWithValue("@recaptcha_private_key", reCaptcha.ReCaptchaPrivateKey);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE settings SET value = @recaptcha_minimum_score WHERE setting_id = 'recaptcha_minimum_score'";
                cmd.Parameters.AddWithValue("@recaptcha_minimum_score",
                    reCaptcha.ReCaptchaMinimumScore.ToString("F", CultureInfo.InvariantCulture));
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM phone_allowlist";
                cmd.ExecuteNonQuery();

                foreach (var item in auth.PhoneAllowedList)
                {
                    cmd.CommandText = "INSERT INTO phone_allowlist VALUES(@country_code, @max_numbers)";
                    cmd.Parameters.AddWithValue("@country_code", item.CountryCode);
                    cmd.Parameters.AddWithValue("@max_numbers", item.MaxNumbers);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                auth.SetSmtpEmail();

                mySqlTransaction.Commit();
                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // USERS

    public OperationResult CheckAdminExists()
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);


            // Connection start - MySql
            mySqlConnection.Open();

            string cmdString = "SELECT 'admin' FROM users u " +
                               "JOIN roles r ON u.role_id = r.role_id " +
                               "WHERE r.role_name = 'admin' " +
                               "LIMIT 1";

            MySqlCommand cmd = new MySqlCommand(cmdString, mySqlConnection);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckUsernameExists(string username)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT username FROM users WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckEmailExists(string email)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT email FROM users WHERE email = @email";
            cmd.Parameters.AddWithValue("@email", email);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckPhoneNumberExists(string phoneCountry, string phoneNumber)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT phone_country, phone_number " +
                              "FROM users " +
                              "WHERE phone_country = @phone_country " +
                              "AND phone_number = @phone_number";
            cmd.Parameters.AddWithValue("@phone_country", phoneCountry);
            cmd.Parameters.AddWithValue("@phone_number", phoneNumber);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult Register(RegistrationModel newUser, bool createAdmin = false)
    {
        if (string.IsNullOrEmpty(newUser.Username) ||
            string.IsNullOrEmpty(newUser.Password))
        {
            return new OperationResult(false, "Error");
        }

        string passwordHashed = BCrypt.Net.BCrypt.EnhancedHashPassword(newUser.Password);

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                string cmdPart1 = "INSERT INTO users(username, password";
                string cmdPart2 = "VALUES(@username, @password";

                if (!string.IsNullOrEmpty(newUser.Email))
                {
                    cmdPart1 += ", email";
                    cmdPart2 += ", @email";
                }

                if (!string.IsNullOrEmpty(newUser.PhoneCountry) &&
                    !string.IsNullOrEmpty(newUser.PhoneNumber))
                {
                    cmdPart1 += ", phone_country, phone_number";
                    cmdPart2 += ", @phone_country, @phone_number";
                }

                if (createAdmin)
                {
                    cmdPart1 += ", role_id";
                    cmdPart2 += ", 0";
                }

                cmdPart1 += ")";
                cmdPart2 += ")";

                MySqlCommand cmd = new MySqlCommand($"", mySqlConnection, mySqlTransaction);
                cmd.CommandText = $"{cmdPart1} {cmdPart2}";
                cmd.Parameters.AddWithValue("@username", newUser.Username);
                cmd.Parameters.AddWithValue("@password", passwordHashed);

                if (!string.IsNullOrEmpty(newUser.Email))
                {
                    cmd.Parameters.AddWithValue("@email", newUser.Email);
                }

                if (!string.IsNullOrEmpty(newUser.PhoneCountry) &&
                    !string.IsNullOrEmpty(newUser.PhoneNumber))
                {
                    cmd.Parameters.AddWithValue("@phone_country", newUser.PhoneCountry);
                    cmd.Parameters.AddWithValue("@phone_number", newUser.PhoneNumber);
                }

                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                ulong userId = (ulong)cmd.ExecuteScalar();

                cmd.CommandText = "INSERT INTO events(type, valid_time, user_id) " +
                                  "VALUES(@type, @valid_time, @user_id)";
                cmd.Parameters.AddWithValue("@type", "registration");
                
                cmd.Parameters.AddWithValue("@valid_time", DateTime.UtcNow.AddHours(24));
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();

                if (Cms.CreateAdmin && createAdmin)
                    Cms.CreateAdmin = false;

                mySqlConnection.Close();
                return new OperationResult(true, "", userId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetUserId(string username)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT user_id FROM users WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            ulong? userId = (ulong?)cmd.ExecuteScalar();
            bool status = userId != null;
            if (status)
            {
                mySqlConnection.Close();
                return new OperationResult(true, "", userId);
            }
            else
            {
                mySqlConnection.Close();
                return new OperationResult(false, "Not found");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetUserIdFromAccessToken(string accessToken)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText =
                "SELECT user_id " +
                "FROM access_tokens " +
                "WHERE value = @value";
            cmd.Parameters.AddWithValue("@value", accessToken);

            ulong? userId = (ulong?)cmd.ExecuteScalar();

            if (userId == null)
            {
                mySqlConnection.Close();
                return new OperationResult(false, "Not found");
            }

            mySqlConnection.Close();
            return new OperationResult(true, "", userId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetUserStatus(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT status FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", id);
            string? userStatus = (string?)cmd.ExecuteScalar();
            bool status = userStatus != null;
            if (status)
            {
                mySqlConnection.Close();
                return new OperationResult(true, "", userStatus);
            }
            else
            {
                mySqlConnection.Close();
                return new OperationResult(false, "Not found");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetUserStatusFromAccessToken(string accessToken)
    {
        var getUserId = GetUserIdFromAccessToken(accessToken);
        if (getUserId.Status.Equals(false) || getUserId.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        ulong userId = (ulong)getUserId.Result;
        return GetUserStatus(userId);
    }

    public async Task<OperationResult> GetUserStatusFromAccessToken(string accessToken, Role role)
    {
        var getUserId = GetUserIdFromAccessToken(accessToken);
        if (getUserId.Status.Equals(false) || getUserId.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        ulong userId = (ulong)getUserId.Result;

        var getUserStatus = GetUserStatus(userId);
        var getUserRole = await GetUser(userId);

        var status = getUserStatus.Status
                     && getUserRole.Status
                     && getUserRole.Result != null
                     && ((UserModel)getUserRole.Result).Role == role;

        var message = getUserStatus.Message.Equals("Error")
                      || getUserRole.Message.Equals("Error") ? "Error" : "";

        return new OperationResult(status, message, getUserStatus.Result);
    }

    public OperationResult GetUserAuthType(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT CASE " +
                              "WHEN ((phone_country IS NOT NULL AND phone_country != '') " +
                                    "AND (phone_number IS NOT NULL AND phone_country != '')) " +
                                    "AND (email IS NOT NULL AND email != '') " +
                                    "THEN 'email and sms' " +
                              "WHEN ((phone_country IS NOT NULL AND phone_country != '') " +
                                    "AND (phone_number IS NOT NULL AND phone_country != '')) " +
                                    "AND (email IS NULL OR email = '') " +
                                    "THEN 'sms' " +
                              "WHEN ((phone_country IS NULL OR phone_country = '') " +
                                    "OR (phone_number IS NULL OR phone_country = '')) " +
                                    "AND (email IS NOT NULL AND email != '') " +
                                    "THEN 'email' " +
                              "ELSE 'none' END " +
                              "FROM users " +
                              "WHERE user_id=@user_id;";
            cmd.Parameters.AddWithValue("@user_id", id);
            string? userStatus = (string?)cmd.ExecuteScalar();
            bool status = userStatus != null;
            if (status)
            {
                mySqlConnection.Close();
                return new OperationResult(true, "", CmsUtilities.AuthTypeToEnum(userStatus!));
            }
            else
            {
                mySqlConnection.Close();
                return new OperationResult(false, "Not found");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CreateAuthCodes(ulong id, AuthType type)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                DateTime validTime = DateTime.UtcNow.AddMinutes(15);

                int codeEmail = CmsUtilities.GetRandomAuthCode();
                int codeSms = CmsUtilities.GetRandomAuthCode();

                if (type is AuthType.Email or AuthType.EmailAndSms)
                {
                    cmd.CommandText = "INSERT INTO auth_codes(value, valid_time, type, user_id) " +
                                      "VALUES(@value, @valid_time, 'email', @user_id)";
                    cmd.Parameters.AddWithValue("@value", codeEmail);
                    cmd.Parameters.AddWithValue("@valid_time", validTime);
                    cmd.Parameters.AddWithValue("@user_id", id);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                if (type is AuthType.Sms or AuthType.EmailAndSms)
                {
                    cmd.CommandText = "INSERT INTO auth_codes(value, valid_time, type, user_id) " +
                                      "VALUES(@value, @valid_time, 'sms', @user_id)";
                    cmd.Parameters.AddWithValue("@value", codeSms);
                    cmd.Parameters.AddWithValue("@valid_time", validTime);
                    cmd.Parameters.AddWithValue("@user_id", id);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true, "", (codeEmail, codeSms));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetUserContact(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);

            cmd.CommandText = "SELECT email FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", id);
            var emailRaw = cmd.ExecuteScalar();
            string? email;
            if (emailRaw is DBNull) email = null;
            else email = (string)emailRaw;
            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT phone_country FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", id);
            var phoneCountryRaw = cmd.ExecuteScalar();
            string? phoneCountry;
            if (phoneCountryRaw is DBNull) phoneCountry = null;
            else phoneCountry = (string)phoneCountryRaw;
            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT phone_number FROM users WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", id);
            var phoneNumberRaw = cmd.ExecuteScalar();
            string? phoneNumber;
            if (phoneNumberRaw is DBNull) phoneNumber = null;
            else phoneNumber = (string)phoneNumberRaw;
            cmd.Parameters.Clear();

            mySqlConnection.Close();
            return new OperationResult(true, "", (email, phoneCountry, phoneNumber));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckAuthCode(ulong id, int code, string type)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);

            cmd.CommandText = "SELECT code_id FROM auth_codes " +
                              "WHERE user_id = @user_id " +
                              "AND value = @value " +
                              "AND type = @type " +
                              "LIMIT 1";

            cmd.Parameters.AddWithValue("@user_id", id);
            cmd.Parameters.AddWithValue("@value", code);
            cmd.Parameters.AddWithValue("@type", type);
            MySqlDataReader reader = cmd.ExecuteReader();

            bool result = reader.HasRows;
            mySqlConnection.Close();
            return new OperationResult(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> Activate(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "UPDATE users " +
                                  "SET status = 'active' " +
                                  "WHERE user_id = @user_id " +
                                  "AND status = 'registered'";
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT username FROM users WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@user_id", id);
                string username = (string)cmd.ExecuteScalar();

                string query = "CREATE (n:User {" +
                               "username: $username, " +
                               "avatar: $avatar, " +
                               "userId: $userId, " +
                               "reputation: $reputation}) " +
                               "RETURN 'Success', n.userId";

                var parameters = new
                {
                    username = username,
                    avatar = "core/user.png",
                    userId = (long)id,
                    reputation = Cms.System?.StartingReputation ?? 1000
                };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);
                var neo4jResult = await neo4jCmd.SingleAsync();
                string neo4jStatus = neo4jResult[0].As<string>();
                long neo4jUserId = neo4jResult[1].As<long>();

                if (neo4jStatus.Equals("Success").Equals(false) ||
                    neo4jUserId.Equals((long)id).Equals(false))
                    throw new Exception("Error: Account activation - invalid neo4j data.");

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult Login(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                string accessToken = Guid.NewGuid().ToString() + $"/{id}";

                cmd.CommandText = "INSERT INTO login_history(user_id) VALUES(@user_id)";
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                ulong loginHistoryId = (ulong)cmd.ExecuteScalar();

                cmd.CommandText = "INSERT INTO access_tokens (user_id, value, login_record_id) " +
                                  "VALUES (@user_id, @value, @login_record_id)";
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.Parameters.AddWithValue("@value", accessToken);
                cmd.Parameters.AddWithValue("@login_record_id", loginHistoryId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true, "", accessToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetUser(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText =
                "SELECT r.role_name " +
                "FROM users u " +
                "JOIN roles r " +
                "ON u.role_id = r.role_id " +
                "WHERE u.user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", id);

            string role = (string)cmd.ExecuteScalar();

            string query = "MATCH (n:User) WHERE n.userId = $userId RETURN n.username, n.avatar, n.userId, n.reputation";
            var parameter = new { userId = (long)id };
            var neo4jCmd = await neo4jSession.RunAsync(query, parameter);
            var neo4jResult = await neo4jCmd.SingleAsync();

            string username = neo4jResult[0].As<string>();
            string avatar = neo4jResult[1].As<string>();
            long userId = neo4jResult[2].As<long>();
            int reputation = neo4jResult[3].As<int>();

            UserModel userModel = new UserModel();
            userModel.Username = username;
            userModel.Avatar = avatar;
            userModel.UserId = (ulong)userId;
            userModel.Reputation = reputation;
            userModel.Role = CmsUtilities.RoleToEnum(role);

            await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);

            return new OperationResult(true, "", userModel);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetUser(string username)
    {
        var getId = GetUserId(username);
        if (getId is { Status: true, Result: ulong })
        {
            return await GetUser((ulong)getId.Result);
        }
        else
        {
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetUserFromAccessToken(string accessToken)
    {
        var getId = GetUserIdFromAccessToken(accessToken);
        if (getId is { Status: true, Result: ulong })
        {
            return await GetUser((ulong)getId.Result);
        }
        else
        {
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult ChangePassword(ulong id, string password)
    {
        string passwordHashed = BCrypt.Net.BCrypt.EnhancedHashPassword(password);

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "UPDATE users SET password = @password WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@password", passwordHashed);
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult CheckLoginData(string username, string password)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT password FROM users WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            var passwordHashedRaw = cmd.ExecuteScalar();
            string? passwordHashed;
            if (passwordHashedRaw is DBNull) passwordHashed = null;
            else passwordHashed = (string)passwordHashedRaw;
            cmd.Parameters.Clear();

            if (passwordHashed == null)
            {
                mySqlConnection.Close();
                return new OperationResult(false, "Not found.");
            }

            cmd.CommandText = "SELECT user_id FROM users WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            ulong userId = (ulong)cmd.ExecuteScalar();

            bool result = BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHashed);

            mySqlConnection.Close();
            return new OperationResult(result, "", userId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult RevokeAccessToken(string accessToken)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM access_tokens WHERE value = @value";
                cmd.Parameters.AddWithValue("@value", accessToken);

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult RevokeAllAccessTokens(ulong id)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM access_tokens WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpdatePassword(string accessToken, string newPassword)
    {
        var getUser = await GetUserFromAccessToken(accessToken);
        var getStatus = GetUserStatusFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null ||
            getStatus.Status.Equals(false) || getStatus.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;
        var userStatus = (string)getStatus.Result;

        if (userStatus.Equals("active").Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                var hashedNewPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "UPDATE users SET password = @password WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@password", hashedNewPassword);
                cmd.Parameters.AddWithValue("@user_id", user.UserId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                return new OperationResult(true);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpdateEmail(string accessToken, string newEmail)
    {
        var getUser = await GetUserFromAccessToken(accessToken);
        var getStatus = GetUserStatusFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null ||
            getStatus.Status.Equals(false) || getStatus.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;
        var userStatus = (string)getStatus.Result;

        if (userStatus.Equals("active").Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "UPDATE users SET email = @email WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@email", newEmail);
                cmd.Parameters.AddWithValue("@user_id", user.UserId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpdatePhone(string accessToken, string phoneCountry, string phoneNumber)
    {
        var getUser = await GetUserFromAccessToken(accessToken);
        var getStatus = GetUserStatusFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null ||
            getStatus.Status.Equals(false) || getStatus.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;
        var userStatus = (string)getStatus.Result;

        if (userStatus.Equals("active").Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "UPDATE users SET phone_country = @phone_country, phone_number = @phone_number WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@phone_country", phoneCountry);
                cmd.Parameters.AddWithValue("@phone_number", phoneNumber);
                cmd.Parameters.AddWithValue("@user_id", user.UserId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpdateAvatar(string accessToken, string fileName)
    {
        var getUser = await GetUserFromAccessToken(accessToken);
        var getStatus = GetUserStatusFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null ||
            getStatus.Status.Equals(false) || getStatus.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;
        var userStatus = (string)getStatus.Result;

        if (userStatus.Equals("active").Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var query = "MATCH (u:User) WHERE u.userId = $userId SET u.avatar = $avatar";
                var parameters = new { userId = (long?)user.UserId, avatar = fileName };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> ChangeRole(string accessToken, ulong userId, Role role)
    {
        var getAdmin = await GetUserFromAccessToken(accessToken);

        if (getAdmin.Status.Equals(false) || getAdmin.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var admin = (UserModel)getAdmin.Result;

        if (admin.Role != Role.Admin)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                var roleName = CmsUtilities.RoleToString(role);
                cmd.CommandText = "SELECT role_id FROM roles WHERE role_name = @role_name";
                cmd.Parameters.AddWithValue("@role_name", roleName);
                var roleId = (uint)cmd.ExecuteScalar();
                cmd.CommandText = "UPDATE users SET role_id = @role_id WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@role_id", roleId);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeleteUser(string accessToken, ulong? userIdToDelete = null)
    {
        var getUser = await GetUserFromAccessToken(accessToken);
        var getStatus = GetUserStatusFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null ||
            getStatus.Status.Equals(false) || getStatus.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;
        var userStatus = (string)getStatus.Result;

        if (userStatus.Equals("active").Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        ulong userId;

        if (userIdToDelete == null)
        {
            if (user.UserId == null) return new OperationResult(false, "Error");

            userId = (ulong)user.UserId;
        }
        else
        {
            if (user.Role != Role.Admin) return new OperationResult(false, "Error");

            userId = (ulong)userIdToDelete;
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                // Can't delete moderator/admin if he has locked reports

                cmd.CommandText = "SELECT user_id FROM users WHERE user_id = @user_id " +
                                  "AND user_id IN (SELECT DISTINCT user_id FROM events WHERE type = 'locked report')";
                cmd.Parameters.AddWithValue("@user_id", userId);
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }
                reader.Close();

                // Images to delete

                List<string> imagesToDelete = new List<string>();

                if (user.Avatar != null && user.Avatar != "core/user.png") imagesToDelete.Add(user.Avatar);

                var queryPictures = "MATCH (u:User)-[:IS_AUTHOR]->(p:Post) " +
                                    "WHERE u.userId = $userId AND p.img <> 'core/content.png' " +
                                    "RETURN p.img";

                var parameters = new { userId = (long)userId };

                var neo4jCmd = await neo4jTransaction.RunAsync(queryPictures, parameters);
                var neo4jResult = await neo4jCmd.ToListAsync();

                foreach (var record in neo4jResult)
                {
                    imagesToDelete.Add(record["p.img"].As<string>());
                }

                // Delete all user's data

                cmd.Parameters.Clear();
                cmd.CommandText = "DELETE FROM users WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();

                var queryDelete = "MATCH (u:User) " +
                                  "WHERE u.userId = $userId " +
                                  "OPTIONAL MATCH (u)-[rAuthorPost:IS_AUTHOR]->(p:Post) " +
                                  "OPTIONAL MATCH (u)-[rAuthorComment:IS_AUTHOR]->(c:Comment) " +
                                  "OPTIONAL MATCH (:POST)-[rCommentUser:HAS_COMMENT]->(c) " +
                                  "OPTIONAL MATCH (:User)-[rLikeOthers:LIKE]->(p) " +
                                  "OPTIONAL MATCH (:User)-[rDislikeOthers:DISLIKE]->(p) " +
                                  "OPTIONAL MATCH (p)-[rCommentOthers:HAS_COMMENT]->(cOthers:Comment) " +
                                  "OPTIONAL MATCH (:User)-[rAuthorCommentOthers:IS_AUTHOR]->(cOthers) " +
                                  "OPTIONAL MATCH (p)-[rTag:HAS_TAG]->(:Tag) " +
                                  "OPTIONAL MATCH (f:Fact)-[rFact:ABOUT]->(p) " +
                                  "OPTIONAL MATCH (u)-[rLikeUser:LIKE]->(:Post) " +
                                  "OPTIONAL MATCH (u)-[rDislikeUser:DISLIKE]->(:Post) " +
                                  "DETACH DELETE u, rAuthorPost, p, rAuthorComment, c, rCommentUser, rLikeOthers, " +
                                  "rDislikeOthers, rCommentOthers, cOthers, rAuthorCommentOthers, rTag, f, rFact, " +
                                  "rLikeUser, rDislikeUser";

                neo4jCmd = await neo4jTransaction.RunAsync(queryDelete, parameters);

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(true, "", imagesToDelete);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetLoginHistory(ulong userId, string accessToken)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            List<(DateTime, bool)> loginHistory = new List<(DateTime, bool)>();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);

            cmd.CommandText = "SELECT l.login_datetime, NOT ISNULL(a.token_id) " +
                              "FROM login_history l " +
                              "LEFT JOIN access_tokens a " +
                              "ON a.login_record_id = l.record_id " +
                              "AND a.value = @value " +
                              "WHERE l.user_id = @user_id " +
                              "ORDER BY l.record_id DESC " +
                              "LIMIT 20";

            cmd.Parameters.AddWithValue("@value", accessToken);
            cmd.Parameters.AddWithValue("@user_id", userId);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                loginHistory.Add((reader.GetDateTime(0), reader.GetBoolean(1)));
            }

            mySqlConnection.Close();
            return new OperationResult(true, "", loginHistory);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // POSTS

    public async Task<OperationResult> CreatePost(string accessToken, PostModel post, List<string> tags, GeoPointModel? geoPoint)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                UserModel user = (UserModel)getUser.Result;

                if (user.UserId == null)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                var getUserStatus = GetUserStatus((ulong)user.UserId);

                if (getUserStatus.Result == null || getUserStatus.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                string status = (string)getUserStatus.Result;
                if (status != "active")
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                string title = post.Title;
                string content = post.Content;
                string img = post.Img;

                double? latitude = geoPoint?.Latitude;
                double? longitude = geoPoint?.Longitude;

                bool blockSearchEngines;
                bool autoReport;

                if (Cms.System != null && user.Reputation <= Cms.System.ReputationUnlisted && user.Role < Role.FactChecker)
                {
                    blockSearchEngines = true;
                }
                else
                {
                    blockSearchEngines = false;
                }

                if (Cms.System is { AutoReport: true } && user.Reputation <= Cms.System.AutoReportReputation
                                                       && user.Role < Role.FactChecker)
                {
                    autoReport = true;
                }
                else
                {
                    autoReport = false;
                }

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "INSERT INTO posts (user_id) VALUES (@user_id)";
                cmd.Parameters.AddWithValue("@user_id", user.UserId);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                ulong postId = (ulong)cmd.ExecuteScalar();

                string queryPost = "MATCH (u:User) WHERE u.userId = $userId " +
                                   "CREATE (p:Post {postId: $postId, date: $date, title: $title, content: $content, img: $img, " +
                                   "blockSearchEngines: $blockSearchEngines, counter: 0, " +
                                   "latitude: $latitude, longitude: $longitude})<-[:IS_AUTHOR]-(u)";

                var parametersPost = new
                {
                    userId = (long)user.UserId,
                    postId = (long)postId,
                    date = DateTime.UtcNow,
                    title = title,
                    content = content,
                    img = img,
                    blockSearchEngines = blockSearchEngines,
                    latitude = latitude,
                    longitude = longitude
                };

                await neo4jTransaction.RunAsync(queryPost, parametersPost);

                foreach (var tag in tags)
                {
                    string queryTag = "MERGE (t:Tag {name: $name}) WITH t " +
                                      "MATCH (p:Post) WHERE p.postId = $postId " +
                                      "CREATE (p)-[:HAS_TAG]->(t)";
                    var parametersTag = new { name = tag, postId = (long)postId };
                    await neo4jTransaction.RunAsync(queryTag, parametersTag);
                }

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);

                if (autoReport)
                {
                    await CreateReport(ReportType.Moderator, (long)postId, Content.Post, "",
                        "Auto reported (low reputation)", true);
                }

                return new OperationResult(true, "", postId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetPost(long postId, string? accessToken = null)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            PostModel post = new PostModel();

            if (accessToken != null)
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    return new OperationResult(false, "Error");
                }

                var userId = ((UserModel)getUser.Result).UserId;

                if (userId == null)
                {
                    return new OperationResult(false, "Error");
                }

                var getUserStatus = GetUserStatus((ulong)userId);

                if (getUserStatus.Result == null || getUserStatus.Result.Equals("banned") ||
                    getUserStatus.Status.Equals(false))
                {
                    return new OperationResult(false, "Error");
                }

                string queryRelation = "MATCH (u:User), (p:Post) " +
                                       "WHERE u.userId = $userId AND p.postId = $postId " +
                                       "RETURN " +
                                       "EXISTS((u)-[:LIKE]->(p)) AS like, " +
                                       "EXISTS((u)-[:DISLIKE]->(p)) AS dislike";
                var parametersRelation = new { userId = (long)userId, postId = postId };
                var neo4jCmdRelation = await neo4jSession.RunAsync(queryRelation, parametersRelation);
                var neo4jResultRelation = await neo4jCmdRelation.SingleAsync();

                post.SelectedUp = neo4jResultRelation["like"].As<bool>();
                post.SelectedDown = neo4jResultRelation["dislike"].As<bool>();
            }

            string queryPost = "MATCH (u:User)-[:IS_AUTHOR]->(p:Post) " +
                               "WHERE p.postId = $postId " +
                               "RETURN p.postId, u.username, u.userId, p.date, p.title, " +
                               "p.content, p.img, p.counter, p.blockSearchEngines";

            var parametersPost = new { postId = postId };

            var neo4jCmdPost = await neo4jSession.RunAsync(queryPost, parametersPost);
            var neo4jResultPost = await neo4jCmdPost.SingleAsync();

            post.PostId = neo4jResultPost["p.postId"].As<long>();
            post.Author = neo4jResultPost["u.username"].As<string>();
            post.AuthorId = neo4jResultPost["u.userId"].As<long>();
            var date = neo4jResultPost["p.date"].As<ZonedDateTime>();
            post.Date = date.ToDateTimeOffset().DateTime;
            post.Title = neo4jResultPost["p.title"].As<string>();
            post.Content = neo4jResultPost["p.content"].As<string>();
            post.Img = neo4jResultPost["p.img"].As<string>();
            post.Counter = neo4jResultPost["p.counter"].As<int>();
            post.BlockSearchEngines = neo4jResultPost["p.blockSearchEngines"].As<bool>();

            string queryFactExists = "MATCH (p:Post) " +
                                     "WHERE p.postId = $postId " +
                                     "RETURN EXISTS((:Fact)-[:ABOUT]->(p)) AS factExists";
            var parametersFactExists = new { postId = postId };
            var neo4jCmdFactExists = await neo4jSession.RunAsync(queryFactExists, parametersFactExists);
            var neo4jResultFactExists = await neo4jCmdFactExists.SingleAsync();

            bool factExists = neo4jResultFactExists[0].As<bool>();

            if (factExists)
            {
                string queryFact = "MATCH (p:Post)<-[:ABOUT]-(f:Fact) " +
                                   "WHERE p.postId = $postId " +
                                   "RETURN f.content";
                var parametersFact = new { postId = postId };
                var neo4jCmdFact = await neo4jSession.RunAsync(queryFact, parametersFact);
                var neo4jResultFact = await neo4jCmdFact.SingleAsync();

                post.Fact = neo4jResultFact["f.content"].As<string>();
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", post);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetPosts(ViewMode view = ViewMode.Best24, List<string>? tags = null, GeoPointModel? geoPoint = null, List<long>? alreadyLoaded = null, string? username = null, string? accessToken = null)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            List<PostModel> posts = new List<PostModel>();

            bool filterTags = false;
            string tagVariant1 = "";
            string tagVariant2 = "";

            bool filterLoaded = false;
            string loadedVariant = "";

            bool filterGeo = false;
            string geoVariant = "";

            bool filterUsername = false;
            string usernameVariant = "";

            string viewVariant1 = "";
            string viewVariant2 = "";

            if (tags != null && tags.Count != 0)
            {
                filterTags = true;
                tagVariant1 = "-[:HAS_TAG]->(t:Tag)";
                tagVariant2 = "t.name IN $tags";
            }

            if (alreadyLoaded != null && alreadyLoaded.Count != 0)
            {
                filterLoaded = true;
                loadedVariant = "NOT p.postId IN $loaded";
            }

            if (geoPoint != null)
            {
                filterGeo = true;
                geoVariant = "point.distance(" +
                             "point({latitude: p.latitude, longitude: p.longitude}), " +
                             "point({latitude: $latitude, longitude: $longitude})) " +
                             "<= $maxDistance";
            }

            if (string.IsNullOrEmpty(username).Equals(false))
            {
                filterUsername = true;
                usernameVariant = "u.username = $username";
            }

            if (view == ViewMode.Best24)
            {
                viewVariant1 = "p.date >= $date";
                viewVariant2 = "p.counter DESC";
            }

            if (view == ViewMode.New)
            {
                viewVariant1 = "";
                viewVariant2 = "p.date DESC";
            }

            string where = filterTags || filterLoaded || filterGeo || filterUsername || view == ViewMode.Best24 ? "WHERE" : "";
            string and1 = filterTags && (filterLoaded || filterGeo || filterUsername || view == ViewMode.Best24) ? "AND" : "";
            string and2 = filterLoaded && (filterGeo || filterUsername || view == ViewMode.Best24) ? "AND" : "";
            string and3 = view == ViewMode.Best24 && (filterGeo || filterUsername) ? "AND" : "";
            string and4 = filterGeo && filterUsername ? "AND" : "";

            string queryPosts = $"MATCH (u:User)-[:IS_AUTHOR]->(p:Post){tagVariant1} " +
                                $"{where} {tagVariant2} {and1} {loadedVariant} {and2} {viewVariant1} " +
                                $"{and3} {geoVariant} {and4} {usernameVariant} " +
                                $"RETURN DISTINCT p.postId, u.username, u.userId, p.date, p.title, " +
                                $"p.content, p.img, p.counter, p.blockSearchEngines " +
                                $"ORDER BY " +
                                $"{viewVariant2} " +
                                $"LIMIT 20";

            var parametersPosts = new
            {
                tags = tags,
                loaded = alreadyLoaded,
                date = DateTime.UtcNow.AddDays(-1),
                latitude = geoPoint?.Latitude,
                longitude = geoPoint?.Longitude,
                maxDistance = geoPoint?.Meters,
                username = username
            };

            var neo4jCmdPosts = await neo4jSession.RunAsync(queryPosts, parametersPosts);
            var neo4jResultPosts = await neo4jCmdPosts.ToListAsync();

            foreach (var record in neo4jResultPosts)
            {
                var post = new PostModel();
                post.PostId = record["p.postId"].As<long>();
                post.Author = record["u.username"].As<string>();
                post.AuthorId = record["u.userId"].As<long>();
                var date = record["p.date"].As<ZonedDateTime>();
                post.Date = date.ToDateTimeOffset().DateTime;
                post.Title = record["p.title"].As<string>();
                post.Content = record["p.content"].As<string>();
                post.Img = record["p.img"].As<string>();
                post.Counter = record["p.counter"].As<int>();
                post.BlockSearchEngines = record["p.blockSearchEngines"].As<bool>();

                if (accessToken != null)
                {
                    var getUser = await GetUserFromAccessToken(accessToken);

                    if (getUser.Result == null || getUser.Status.Equals(false))
                    {
                        return new OperationResult(false, "Error");
                    }

                    var userId = ((UserModel)getUser.Result).UserId;

                    if (userId == null)
                    {
                        return new OperationResult(false, "Error");
                    }

                    var getUserStatus = GetUserStatus((ulong)userId);

                    if (getUserStatus.Result == null || getUserStatus.Result.Equals("banned") ||
                        getUserStatus.Status.Equals(false))
                    {
                        return new OperationResult(false, "Error");
                    }

                    string queryRelation = "MATCH (u:User), (p:Post) " +
                                           "WHERE u.userId = $userId AND p.postId = $postId " +
                                           "RETURN " +
                                           "EXISTS((u)-[:LIKE]->(p)) AS like, " +
                                           "EXISTS((u)-[:DISLIKE]->(p)) AS dislike";

                    var parametersRelation = new { userId = (long)userId, postId = post.PostId };
                    var neo4jCmdRelation = await neo4jSession.RunAsync(queryRelation, parametersRelation);
                    var neo4jResultRelation = await neo4jCmdRelation.SingleAsync();

                    post.SelectedUp = neo4jResultRelation["like"].As<bool>();
                    post.SelectedDown = neo4jResultRelation["dislike"].As<bool>();
                }

                posts.Add(post);
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, $"{(posts.Count == 0 ? "Empty" : "")}", posts);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetTags()

    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            string query = "MATCH (:Post)-[:HAS_TAG]->(t:Tag) RETURN DISTINCT t.name ORDER BY t.name ASC";

            var neo4jCmd = await neo4jSession.RunAsync(query);
            var neo4jResult = await neo4jCmd.ToListAsync();

            List<string> tags = new List<string>();

            foreach (var record in neo4jResult)
            {
                tags.Add(record[0].As<string>());
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", tags);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> Upvote(PostModel post, string accessToken)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver);
                    return new OperationResult(false, "Error");
                }

                var userId = ((UserModel)getUser.Result).UserId;

                var query = "MATCH (u:User)-[r:DISLIKE]->(p:Post)<-[:IS_AUTHOR]-(a:User) " +
                            "WHERE u.userId = $userId AND p.postId = $postId " +
                            "SET p.counter = p.counter + 1, a.reputation = a.reputation + 1 " +
                            "DELETE r";

                var parameters = new { userId = (long)userId, postId = post.PostId };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                query = "MATCH (u:User), (p:Post)<-[:IS_AUTHOR]-(a:User) " +
                        "WHERE u.userId = $userId AND p.postId = $postId " +
                        "SET p.counter = p.counter + 1, a.reputation = a.reputation + 1 " +
                        "CREATE (u)-[:LIKE]->(p)";

                neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true, "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> Downvote(PostModel post, string accessToken)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver);
                    return new OperationResult(false, "Error");
                }

                var userId = ((UserModel)getUser.Result).UserId;

                var query = "MATCH (u:User)-[r:LIKE]->(p:Post)<-[:IS_AUTHOR]-(a:User) " +
                            "WHERE u.userId = $userId AND p.postId = $postId " +
                            "SET p.counter = p.counter - 1, a.reputation = a.reputation - 1 " +
                            "DELETE r";

                var parameters = new { userId = (long)userId, postId = post.PostId };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                query = "MATCH (u:User), (p:Post)<-[:IS_AUTHOR]-(a:User) " +
                        "WHERE u.userId = $userId AND p.postId = $postId " +
                        "SET p.counter = p.counter - 1, a.reputation = a.reputation - 1 " +
                        "CREATE (u)-[:DISLIKE]->(p)";

                neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true, "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UpvoteOff(PostModel post, string accessToken)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver);
                    return new OperationResult(false, "Error");
                }

                var userId = ((UserModel)getUser.Result).UserId;

                var query = "MATCH (u:User)-[r:LIKE]->(p:Post)<-[:IS_AUTHOR]-(a:User) " +
                            "WHERE u.userId = $userId AND p.postId = $postId " +
                            "SET p.counter = p.counter - 1, a.reputation = a.reputation - 1 " +
                            "DELETE r";

                var parameters = new { userId = (long)userId, postId = post.PostId };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true, "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DownvoteOff(PostModel post, string accessToken)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUser = await GetUserFromAccessToken(accessToken);

                if (getUser.Result == null || getUser.Status.Equals(false))
                {
                    await CloseConnection(neo4jSession, neo4jDriver);
                    return new OperationResult(false, "Error");
                }

                var userId = ((UserModel)getUser.Result).UserId;

                var query = "MATCH (u:User)-[r:DISLIKE]->(p:Post)<-[:IS_AUTHOR]-(a:User) " +
                            "WHERE u.userId = $userId AND p.postId = $postId " +
                            "SET p.counter = p.counter + 1, a.reputation = a.reputation + 1 " +
                            "DELETE r";

                var parameters = new { userId = (long)userId, postId = post.PostId };

                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true, "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeletePost(PostModel post, string accessToken)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Result == null || getUser.Status.Equals(false))
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var user = (UserModel)getUser.Result;

                if (user.UserId == null)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                if (((long)(user.UserId) != post.AuthorId) && user.Role < Role.Moderator)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                var query = "MATCH (p:Post) " +
                            "WHERE p.postId = $postId " +
                            "OPTIONAL MATCH (:User)-[rAuthor:IS_AUTHOR]->(p) " +
                            "OPTIONAL MATCH (:User)-[rLike:LIKE]->(p) " +
                            "OPTIONAL MATCH (:User)-[rDislike:DISLIKE]->(p) " +
                            "OPTIONAL MATCH (p)-[rComment:HAS_COMMENT]->(c:Comment) " +
                            "OPTIONAL MATCH (p)-[rTag:HAS_TAG]->(:Tag) " +
                            "OPTIONAL MATCH (f:Fact)-[rFact:ABOUT]->(p) " +
                            "DETACH DELETE p, c, f, rAuthor, rLike, rDislike, rComment, rTag, rFact";

                var parameters = new { postId = post.PostId };
                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM posts WHERE post_id = @postId";
                cmd.Parameters.AddWithValue("@postId", (ulong)post.PostId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetFact(long postId)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            string fact = "";

            string queryFactExists = "MATCH (p:Post) " +
                                     "WHERE p.postId = $postId " +
                                     "RETURN EXISTS((:Fact)-[:ABOUT]->(p)) AS factExists";
            var parametersFactExists = new { postId = postId };
            var neo4jCmdFactExists = await neo4jSession.RunAsync(queryFactExists, parametersFactExists);
            var neo4jResultFactExists = await neo4jCmdFactExists.SingleAsync();

            bool factExists = neo4jResultFactExists[0].As<bool>();

            if (factExists)
            {
                string queryFact = "MATCH (p:Post)<-[:ABOUT]-(f:Fact) " +
                                   "WHERE p.postId = $postId " +
                                   "RETURN f.content";
                var parametersFact = new { postId = postId };
                var neo4jCmdFact = await neo4jSession.RunAsync(queryFact, parametersFact);
                var neo4jResultFact = await neo4jCmdFact.SingleAsync();

                fact = neo4jResultFact["f.content"].As<string>();
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", fact);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> CreateFact(string accessToken, long postId, string content)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (user.Role < Role.FactChecker)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var queryDelete = "MATCH (p:Post) " +
                                  "WHERE p.postId = $postId " +
                                  "OPTIONAL MATCH (f:Fact)-[r:ABOUT]->(p) " +
                                  "DETACH DELETE f, r";

                var queryCreate = "MATCH (p:Post) " +
                                  "WHERE p.postId = $postId " +
                                  "CREATE (f:Fact {content: $content})-[:ABOUT]->(p)";

                var parametersDelete = new { postId = postId };
                var parametersCreate = new { postId = postId, content = content };

                var neo4jCmdDelete = await neo4jTransaction.RunAsync(queryDelete, parametersDelete);
                var neo4jCmdCreate = await neo4jTransaction.RunAsync(queryCreate, parametersCreate);

                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeleteFact(string accessToken, long postId)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status.Equals(false) || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (user.Role < Role.FactChecker)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var queryDelete = "MATCH (p:Post) " +
                                  "WHERE p.postId = $postId " +
                                  "OPTIONAL MATCH (f:Fact)-[r:ABOUT]->(p) " +
                                  "DETACH DELETE f, r";

                var parametersDelete = new { postId = postId };

                var neo4jCmdDelete = await neo4jTransaction.RunAsync(queryDelete, parametersDelete);

                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // MESSAGES

    public async Task<OperationResult> GetLatestMessagesUsers(string accessToken)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            var getUserId = GetUserIdFromAccessToken(accessToken);
            if (getUserId.Status == false || getUserId.Result == null)
            {
                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }

            var userId = (ulong)getUserId.Result;

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT sender_id, receiver_id FROM messages WHERE " +
                              "sender_id = @userId OR " +
                              "receiver_id = @userId " +
                              "ORDER BY message_id DESC";
            cmd.Parameters.AddWithValue("@userId", userId);

            MySqlDataReader reader = cmd.ExecuteReader();

            List<ulong> usersIds = new List<ulong>();

            while (reader.Read())
            {
                ulong senderId = reader.GetUInt64("sender_id");
                ulong receiverId = reader.GetUInt64("receiver_id");

                if (senderId != userId)
                {
                    usersIds.Add(senderId);
                }
                else
                {
                    usersIds.Add(receiverId);
                }
            }

            usersIds = usersIds.Distinct().ToList();

            List<UserModel> users = new List<UserModel>();

            foreach (var id in usersIds)
            {
                var getUser = await this.GetUser(id);

                if (getUser.Status == false || getUser.Result == null)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                var user = (UserModel)getUser.Result;

                users.Add(user);
            }

            await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
            return new OperationResult(true, $"{(users.Count == 0 ? "Empty" : "")}", users);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult GetMessages(ulong user1, ulong user2, ulong? maxId = null)

    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT * FROM messages WHERE " +
                              "((sender_id = @user1 AND receiver_id = @user2) OR " +
                              "(sender_id = @user2 AND receiver_id = @user1)) " +
                              $"{(maxId != null ? "AND message_id < @maxId" : "")} " +
                              "ORDER BY message_id DESC LIMIT 10";
            cmd.Parameters.AddWithValue("@user1", user1);
            cmd.Parameters.AddWithValue("@user2", user2);
            if (maxId != null)
            {
                cmd.Parameters.AddWithValue("@maxId", (ulong)maxId);
            }

            List<MessageModel> messages = new List<MessageModel>();

            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var messageId = reader.GetUInt64("message_id");
                var senderId = reader.GetUInt64("sender_id");
                var receiverId = reader.GetUInt64("receiver_id");
                var content = reader.GetString("content");

                var message = new MessageModel();
                message.MessageId = messageId;
                message.SenderId = senderId;
                message.ReceiverId = receiverId;
                message.Content = content;

                messages.Add(message);
            }

            mySqlConnection.Close();
            return new OperationResult(true, $"{(messages.Count == 0 ? "Empty" : "")}", messages);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public OperationResult SendMessage(ulong senderId, ulong receiverId, string message)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "INSERT INTO messages (sender_id, receiver_id, content) " +
                                  "VALUES (@senderId, @receiverId, @content)";

                cmd.Parameters.AddWithValue("@senderId", senderId);
                cmd.Parameters.AddWithValue("@receiverId", receiverId);
                cmd.Parameters.AddWithValue("@content", message);
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                ulong messageId = (ulong)cmd.ExecuteScalar();

                mySqlTransaction.Commit();

                mySqlConnection.Close();
                return new OperationResult(true, "", messageId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetMessage(string accessToken, ulong messageId)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status == false || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            cmd.CommandText = "SELECT * FROM messages WHERE message_id = @messageId";
            cmd.Parameters.AddWithValue("@messageId", messageId);
            MySqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var id = reader.GetUInt64("message_id");
                var senderId = reader.GetUInt64("sender_id");
                var receiverId = reader.GetUInt64("receiver_id");
                var content = reader.GetString("content");

                var message = new MessageModel();
                message.MessageId = id;
                message.SenderId = senderId;
                message.ReceiverId = receiverId;
                message.Content = content;

                if (user.UserId != senderId && user.UserId != receiverId && user.Role < Role.Moderator)
                {
                    mySqlConnection.Close();
                    return new OperationResult(false, "Error");
                }

                mySqlConnection.Close();
                return new OperationResult(true, "", message);
            }

            mySqlConnection.Close();
            return new OperationResult(false);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeleteMessage(MessageModel message, string accessToken)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status == false || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                if (user.UserId != message.SenderId && user.Role < Role.Moderator)
                {
                    mySqlConnection.Close();
                    return new OperationResult(false, "Error");
                }

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM messages WHERE message_id = @messageId";
                cmd.Parameters.AddWithValue("@messageId", message.MessageId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();

                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // COMMENTS

    public async Task<OperationResult> CreateComment(string accessToken, CommentModel comment, long postId)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                var getUserStatus = GetUserStatusFromAccessToken(accessToken);

                if (getUserStatus.Status != true ||
                    getUserStatus.Result == null ||
                    (getUserStatus.Result != null &&
                     ((string)(getUserStatus.Result)).Equals("active").Equals(false)))
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "INSERT INTO comments (user_id) VALUES (@userId)";
                cmd.Parameters.AddWithValue("@userId", comment.AuthorId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                ulong commentId = (ulong)cmd.ExecuteScalar();

                var query = "MATCH (p:Post), (u:User) " +
                            "WHERE p.postId = $postId AND u.userId = $userId " +
                            "CREATE (c:Comment {commentId: $commentId, content: $content, date: $date}) " +
                            "CREATE (p)-[:HAS_COMMENT]->(c), (u)-[:IS_AUTHOR]->(c)";

                var parameters = new
                {
                    postId = postId,
                    userId = comment.AuthorId,
                    commentId = (long)commentId,
                    content = comment.Content,
                    date = comment.Date
                };

                await neo4jTransaction.RunAsync(query, parameters);

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(true, "", commentId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetComments(long postId, long minId = 0)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            var query = "MATCH (p:Post)-[:HAS_COMMENT]->(c:Comment)<-[:IS_AUTHOR]-(u:User) " +
                        "WHERE p.postId = $postId AND c.commentId > $minId " +
                        "RETURN c.commentId, c.content, c.date, u.userId, u.username, u.avatar " +
                        "ORDER BY c.commentId ASC " +
                        "LIMIT 10";

            var parameters = new { postId = postId, minId = minId };

            var neo4jCmd = await neo4jSession.RunAsync(query, parameters);
            var neo4jResult = await neo4jCmd.ToListAsync();

            var comments = new List<CommentModel>();

            foreach (var record in neo4jResult)
            {
                var comment = new CommentModel();
                comment.CommentId = record["c.commentId"].As<long>();
                comment.Content = record["c.content"].As<string>();
                var date = record["c.date"].As<ZonedDateTime>();
                comment.Date = date.ToDateTimeOffset().DateTime;
                comment.AuthorId = record["u.userId"].As<long>();
                comment.Author = record["u.username"].As<string>();
                comment.Avatar = record["u.avatar"].As<string>();

                comments.Add(comment);
            }

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", comments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetComment(long commentId)
    {
        try
        {
            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();

            var query = "MATCH (c:Comment)<-[:IS_AUTHOR]-(u:User) " +
                        "WHERE c.commentId = $commentId " +
                        "RETURN c.commentId, c.content, c.date, u.userId, u.username, u.avatar";

            var parameters = new { commentId = commentId };

            var neo4jCmd = await neo4jSession.RunAsync(query, parameters);
            var neo4jResult = await neo4jCmd.SingleAsync();

            var comment = new CommentModel();
            comment.CommentId = neo4jResult["c.commentId"].As<long>();
            comment.Content = neo4jResult["c.content"].As<string>();
            var date = neo4jResult["c.date"].As<ZonedDateTime>();
            comment.Date = date.ToDateTimeOffset().DateTime;
            comment.AuthorId = neo4jResult["u.userId"].As<long>();
            comment.Author = neo4jResult["u.username"].As<string>();
            comment.Avatar = neo4jResult["u.avatar"].As<string>();

            await CloseConnection(neo4jSession, neo4jDriver);
            return new OperationResult(true, "", comment);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeleteComment(CommentModel comment, string accessToken)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status == false || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection settings - Neo4j
            Uri neo4jUri = new Uri($"neo4j://{_neo4jHostname}");
            var neo4jDriver = GraphDatabase.Driver(neo4jUri,
                AuthTokens.Basic(_neo4jUser, _neo4jPassword));

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            // Connection start - Neo4j
            var neo4jSession = neo4jDriver.AsyncSession();
            var neo4jTransaction = await neo4jSession.BeginTransactionAsync();

            try
            {
                if (user.UserId == null)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                if (((long)(user.UserId) != comment.AuthorId) && user.Role < Role.Moderator)
                {
                    await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                    return new OperationResult(false, "Error");
                }

                var query = "MATCH (c:Comment) " +
                            "WHERE c.commentId = $commentId " +
                            "OPTIONAL MATCH (:User)-[rAuthor:IS_AUTHOR]->(c) " +
                            "OPTIONAL MATCH (:Post)-[rComment:HAS_COMMENT]->(c) " +
                            "DETACH DELETE c, rAuthor, rComment";

                var parameters = new { commentId = comment.CommentId };
                var neo4jCmd = await neo4jTransaction.RunAsync(query, parameters);

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM comments WHERE comment_id = @commentId";
                cmd.Parameters.AddWithValue("@commentId", (ulong)comment.CommentId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                await neo4jTransaction.CommitAsync();
                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                await neo4jTransaction.RollbackAsync();

                await CloseConnection(neo4jSession, neo4jDriver, mySqlConnection);
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    // BANS

    public async Task<OperationResult> CreateReport(ReportType type, long contentId, Content contentType, string accessToken, string reason, bool auto = false)
    {
        ulong? reportCreatorId = null;

        if (auto.Equals(false))
        {
            var getUserStatus = GetUserStatusFromAccessToken(accessToken);
            if (getUserStatus.Status != true ||
                getUserStatus.Result == null ||
                (getUserStatus.Result != null &&
                 ((string)(getUserStatus.Result)).Equals("active").Equals(false)))
            {
                return new OperationResult(false, "Error");
            }

            var getUser = await GetUserFromAccessToken(accessToken);
            if (getUser.Status != true || getUser.Result == null)
            {
                return new OperationResult(false, "Error");
            }

            reportCreatorId = ((UserModel)getUser.Result).UserId;
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                var reportType = CmsUtilities.ReportTypeToString(type);

                if (contentType.Equals(Content.Post))
                {
                    cmd.CommandText = "INSERT INTO reports (type, content, report_creator, reported_post_id) " +
                                      "VALUES (@type, @content, @report_creator, @reported_post_id)";
                    cmd.Parameters.AddWithValue("@type", reportType);
                    cmd.Parameters.AddWithValue("@content", reason);
                    if (reportCreatorId == null) cmd.Parameters.AddWithValue("@report_creator", DBNull.Value);
                    else cmd.Parameters.AddWithValue("@report_creator", reportCreatorId);
                    cmd.Parameters.AddWithValue("@reported_post_id", contentId);
                    cmd.ExecuteNonQuery();
                }
                else if (contentType.Equals(Content.Comment))
                {
                    cmd.CommandText = "INSERT INTO reports (type, content, report_creator, reported_comment_id) " +
                                      "VALUES (@type, @content, @report_creator, @reported_comment_id)";
                    cmd.Parameters.AddWithValue("@type", reportType);
                    cmd.Parameters.AddWithValue("@content", reason);
                    cmd.Parameters.AddWithValue("@report_creator", reportCreatorId);
                    cmd.Parameters.AddWithValue("@reported_comment_id", contentId);
                    cmd.ExecuteNonQuery();
                }
                else if (contentType.Equals(Content.Message))
                {
                    cmd.CommandText = "INSERT INTO reports (type, content, report_creator, reported_message_id) " +
                                      "VALUES (@type, @content, @report_creator, @reported_message_id)";
                    cmd.Parameters.AddWithValue("@type", reportType);
                    cmd.Parameters.AddWithValue("@content", reason);
                    cmd.Parameters.AddWithValue("@report_creator", reportCreatorId);
                    cmd.Parameters.AddWithValue("@reported_message_id", contentId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    mySqlConnection.Close();
                    return new OperationResult(false, "Error");
                }

                mySqlTransaction.Commit();
                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> BanUser(string accessToken, string username, string reason, uint days)
    {
        var getModerator = await GetUserFromAccessToken(accessToken);

        if (getModerator.Status != true || getModerator.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var moderator = (UserModel)getModerator.Result;

        if (moderator.Role < Role.Moderator)
        {
            return new OperationResult(false, "Error");
        }

        var getUserId = GetUserId(username);

        if (getUserId.Status != true || getUserId.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var userId = (ulong)getUserId.Result;

        var getUser = await GetUser(userId);

        if (getUser.Status != true || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (user.Role >= Role.FactChecker)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                var endTime = DateTime.UtcNow.AddDays(days);

                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "INSERT INTO ban_history (reason, date_end, user_id, moderator_id) " +
                                  "VALUES (@reason, @date_end, @user_id, @moderator_id)";

                cmd.Parameters.AddWithValue("@reason", reason);
                cmd.Parameters.AddWithValue("@date_end", endTime);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.Parameters.AddWithValue("@moderator_id", moderator.UserId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                var banId = (ulong)cmd.ExecuteScalar();

                cmd.CommandText = "INSERT INTO events (type, valid_time, user_id, ban_id) " +
                                  "VALUES (@type, @valid_time, @user_id, @ban_id)";
                cmd.Parameters.AddWithValue("@type", "ban");
                cmd.Parameters.AddWithValue("@valid_time", endTime);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.Parameters.AddWithValue("@ban_id", banId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "UPDATE users SET status = 'banned' WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                mySqlConnection.Close();

                RevokeAllAccessTokens(userId);

                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> UnBanUser(string accessToken, string username)
    {
        var getModerator = await GetUserFromAccessToken(accessToken);

        if (getModerator.Status != true || getModerator.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var moderator = (UserModel)getModerator.Result;

        if (moderator.Role < Role.Moderator)
        {
            return new OperationResult(false, "Error");
        }

        var getUserId = GetUserId(username);

        if (getUserId.Status != true || getUserId.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var userId = (ulong)getUserId.Result;

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);
                cmd.CommandText = "DELETE FROM ban_history " +
                                  "WHERE ban_id IN " +
                                  "(SELECT ban_id FROM events " +
                                  "WHERE user_id = @user_id AND type = 'ban' AND ban_id IS NOT NULL)";
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "DELETE FROM events " +
                                  "WHERE user_id = @user_id AND type = 'ban'";
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "UPDATE users SET status = 'active' WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();

                mySqlTransaction.Commit();
                mySqlConnection.Close();

                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetReport(string accessToken, ReportType type)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status != true || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (type == ReportType.Moderator && user.Role < Role.Moderator)
        {
            return new OperationResult(false, "Error");
        }
        else if (type == ReportType.FactChecker && user.Role < Role.FactChecker)
        {
            return new OperationResult(false, "Error");
        }

        var typeName = CmsUtilities.ReportTypeToString(type);

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            var report = new ReportModel();
            ulong? contentId = null;
            Content? contentType = null;

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                cmd.CommandText = "SELECT report_id FROM reports " +
                                  "WHERE status = 'active' " +
                                  "AND type = @type " +
                                  "ORDER BY report_id ASC " +
                                  "LIMIT 1";
                cmd.Parameters.AddWithValue("@type", typeName);
                var reader = cmd.ExecuteReader();
                ulong reportId;
                if (reader.Read())
                {
                    reportId = (ulong)reader["report_id"];
                    reader.Close();
                }
                else
                {
                    mySqlConnection.Close();
                    return new OperationResult(true, "Empty");
                }
                cmd.Parameters.Clear();
                

                cmd.CommandText = "UPDATE reports SET status = 'locked', moderator_id = @moderator_id " +
                                  "WHERE report_id = @report_id";
                cmd.Parameters.AddWithValue("@moderator_id", user.UserId);
                cmd.Parameters.AddWithValue("@report_id", reportId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                var validTime = DateTime.UtcNow.AddMinutes(15);

                cmd.CommandText = "INSERT INTO events (type, valid_time, user_id, report_id) " +
                                  "VALUES ('locked report', @valid_time, @user_id, @report_id)";
                cmd.Parameters.AddWithValue("@valid_time", validTime);
                cmd.Parameters.AddWithValue("@user_id", user.UserId);
                cmd.Parameters.AddWithValue("@report_id", reportId);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = "SELECT * FROM reports WHERE report_id = @report_id";
                cmd.Parameters.AddWithValue("@report_id", reportId);
                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    report.ReportId = (ulong)reader["report_id"];
                    report.Status = (string)reader["status"];
                    report.Type = CmsUtilities.ReportTypeToEnum((string)reader["type"]);
                    report.Content = (string)reader["content"];
                    
                    var reportCreatorId = reader["report_creator"];
                    if (reportCreatorId != DBNull.Value)
                    {
                        report.ReportCreatorId = (ulong)reportCreatorId;
                    }

                    var reportedPostId = reader["reported_post_id"];
                    if (reportedPostId != DBNull.Value)
                    {
                        contentType = Content.Post;
                        contentId = (ulong)reportedPostId;
                    }

                    var reportedCommentId = reader["reported_comment_id"];
                    if (reportedCommentId != DBNull.Value)
                    {
                        contentType = Content.Comment;
                        contentId = (ulong)reportedCommentId;
                    }

                    var reportedMessageId = reader["reported_message_id"];
                    if (reportedMessageId != DBNull.Value)
                    {
                        contentType = Content.Message;
                        contentId = (ulong)reportedMessageId;
                    }

                    reader.Close();
                }

                mySqlTransaction.Commit();
                mySqlConnection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }

            if (contentType == null || contentId == null)
            {
                return new OperationResult(false, "Error");
            }

            if (contentType == Content.Post)
            {
                var getPost = await GetPost((long)contentId, accessToken);

                if (getPost.Status != true || getPost.Result == null)
                {
                    return new OperationResult(false, "Error");
                }

                var post = (PostModel)getPost.Result;
                report.ReportedContent = post;
            }
            else if (contentType == Content.Comment)
            {
                var getComment = await GetComment((long)contentId);

                if (getComment.Status != true || getComment.Result == null)
                {
                    return new OperationResult(false, "Error");
                }

                var comment = (CommentModel)getComment.Result;
                report.ReportedContent = comment;
            }
            else if (contentType == Content.Message)
            {
                var getMessage = await GetMessage(accessToken, (ulong)contentId);

                if (getMessage.Status != true || getMessage.Result == null)
                {
                    return new OperationResult(false, "Error");
                }

                var message = (MessageModel)getMessage.Result;
                report.ReportedContent = message;
            }

            if (report.ReportCreatorId != null)
            {
                var getReportCreator = await GetUser((ulong)report.ReportCreatorId);

                if (getReportCreator.Status && getReportCreator.Result != null)
                {
                    var reportCreator = (UserModel)getReportCreator.Result;
                    report.ReportCreator = reportCreator.Username;
                }
            }

            return new OperationResult(true, "", report);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> DeleteReport(string accessToken, ulong reportId)
    {
        var getUser = await GetUserFromAccessToken(accessToken);

        if (getUser.Status != true || getUser.Result == null)
        {
            return new OperationResult(false, "Error");
        }

        var user = (UserModel)getUser.Result;

        if (user.Role < Role.FactChecker)
        {
            return new OperationResult(false, "Error");
        }

        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();
            MySqlTransaction mySqlTransaction = mySqlConnection.BeginTransaction();

            try
            {
                MySqlCommand cmd = new MySqlCommand("", mySqlConnection, mySqlTransaction);

                cmd.CommandText = "DELETE FROM reports " +
                                  "WHERE report_id = @report_id AND moderator_id = @moderator_id";
                cmd.Parameters.AddWithValue("@report_id", reportId);
                cmd.Parameters.AddWithValue("@moderator_id", user.UserId);
                cmd.ExecuteNonQuery();
                
                mySqlTransaction.Commit();
                mySqlConnection.Close();
                return new OperationResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mySqlTransaction.Rollback();
                mySqlConnection.Close();
                return new OperationResult(false, "Error");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }

    public async Task<OperationResult> GetBanHistory(UserModel user)
    {
        try
        {
            // Connection settings - MySql
            string mySqlConnStr = $"server={_mysqlHostname};" +
                                  $"user={_mysqlUser};" +
                                  $"database={_mysqlDatabase};" +
                                  $"port=3306;" +
                                  $"password={_mysqlPassword}";
            MySqlConnection mySqlConnection = new MySqlConnection(mySqlConnStr);

            // Connection start - MySql
            mySqlConnection.Open();

            List<BanModel> banHistory = new List<BanModel>();

            MySqlCommand cmd = new MySqlCommand("", mySqlConnection);
            
            cmd.CommandText = "SELECT * FROM ban_history " +
                              "WHERE user_id = @user_id";
            cmd.Parameters.AddWithValue("@user_id", user.UserId);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                BanModel ban = new BanModel();
                ban.BanId = (ulong)reader["ban_id"];
                ban.Reason = (string)reader["reason"];
                ban.DateStart = (DateTime)reader["date_start"];
                ban.DateEnd = (DateTime)reader["date_end"];
                ban.UserId = (ulong)reader["user_id"];
                ban.Username = user.Username ?? "";
                var moderatorId = reader["moderator_id"];

                if (moderatorId != DBNull.Value)
                {
                    ban.ModeratorId = (ulong?)moderatorId;
                    var getModerator = await GetUser((ulong)ban.ModeratorId);
                    if (getModerator is { Status: true, Result: UserModel })
                    {
                        var moderator = (UserModel)getModerator.Result;
                        ban.ModeratorName = moderator.Username;
                    }
                }

                banHistory.Add(ban);
            }

            mySqlConnection.Close();
            return new OperationResult(true, "", banHistory);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new OperationResult(false, "Error");
        }
    }
}