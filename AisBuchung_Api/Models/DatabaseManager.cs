using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Data.Sqlite;
using JsonSerializer;
using System.Reflection.Metadata.Ecma335;
using System.ComponentModel.DataAnnotations;

namespace AisBuchung_Api.Models
{
    public class DatabaseManager
    {
        SqliteConnection connection;

        private void OpenConnection()
        {
            var connectionBuilder = new SqliteConnectionStringBuilder { DataSource = Path };
            connection = new SqliteConnection(connectionBuilder.ConnectionString);
            connection.Open();
        }

        private void CloseConnection()
        {
            if (connection == null)
            {
                return;
            }
            connection.Close();
        }

        public void CreateNewDatabase(bool overwrite)
        {
            CloseConnection();

            if (File.Exists(Path))
            {
                if (overwrite)
                {
                    File.Delete(Path);
                }
                else
                {
                    return;
                }
            }

            File.WriteAllText(Path, "");

            var createCommands = new string[]
            {
                $"CREATE TABLE Admins (Id INTEGER PRIMARY KEY)",
                $"CREATE TABLE Buchungen (Id INTEGER PRIMARY KEY AUTOINCREMENT, Veranstaltung INTEGER NOT NULL, Nutzer INTEGER NOT NULL, Buchungstyp INTEGER NOT NULL, Zeitstempel INTEGER NOT NULL)",
                $"CREATE TABLE Emailverifizierungen (Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL, Nutzer INTEGER NOT NULL, Zeitfrist INTEGER NOT NULL)",
                $"CREATE TABLE Kalender (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)",
                $"CREATE TABLE Kalenderberechtigte (Id INTEGER PRIMARY KEY AUTOINCREMENT, Kalender INTEGER NOT NULL, Veranstalter INTEGER NOT NULL)",
                $"CREATE TABLE Nutzerdaten (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nachname TEXT NOT NULL, Vorname TEXT NOT NULL, Email TEXT NOT NULL, Abteilung TEXT NOT NULL, Verifiziert INTEGER NOT NULL)",
                $"CREATE TABLE Teilnehmer (Id INTEGER PRIMARY KEY AUTOINCREMENT, Veranstaltung INTEGER NOT NULL, Nutzer INTEGER NOT NULL)",
                $"CREATE TABLE Veranstalter (Id INTEGER PRIMARY KEY, Passwort TEXT NOT NULL, Autorisiert INTEGER NOT NULL)",
                $"CREATE TABLE Veranstaltungen (Id INTEGER PRIMARY KEY AUTOINCREMENT, Uid TEXT NOT NULL, Anmeldefrist INTEGER NOT NULL, Teilnehmerlimit INTEGER NOT NULL, Teilnehmerzahl INTEGER NOT NULL, Öffentlich INTEGER NOT NULL)",
                $"CREATE TABLE Emailänderungen (Id INTEGER PRIMARY KEY AUTOINCREMENT, Emailverifizierung INTEGER NOT NULL, NeueEmail TEXT NOT NULL)",

            };

            ExecuteNonQuery(createCommands);
        }

        public SqliteDataReader ExecuteReader(string command)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = command;
            var r = c.ExecuteReader();
            return r;
        }

        public SqliteDataReader ExecuteReader(string baseCommand, Parameter parameter)
        {
            return ExecuteReader(baseCommand, new Parameter[] { parameter });
        }

        public SqliteDataReader ExecuteReader(string baseCommand, Parameter[] parameters)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = baseCommand;
            AddParameters(c, parameters);
            return c.ExecuteReader();
        }

        public void ExecuteNonQuery(string baseCommand, Parameter parameter)
        {
            ExecuteNonQuery(baseCommand, new Parameter[] { parameter });
        }

        public void ExecuteNonQuery(string[] commands)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            OpenConnection();
            var command = connection.CreateCommand();
            foreach (var c in commands)
            {
                command.CommandText = c;
                command.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQuery(string baseCommand, Parameter[] parameters)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = baseCommand;
            AddParameters(c, parameters);
            c.ExecuteNonQuery();
            CloseConnection();
        }

        public void ExecuteNonQuery(string command)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = command;
            c.ExecuteNonQuery();
            CloseConnection();
        }

        public int CountResults(string command)
        {
            var r = ExecuteReader(command);
            var result = 0;
            using (r)
            {
                while (r.Read())
                {
                    result += 1;
                }
            }

            CloseConnection();
            return result;
        }

        public int CountResults(string command, Parameter[] parameters)
        {
            var r = ExecuteReader(command, parameters);
            var result = 0;
            using (r)
            {
                while (r.Read())
                {
                    result += 1;
                }
            }

            CloseConnection();
            return result;
        }

        public string ExecuteGet(string table, string uid, Dictionary<string, string> keyTableDictionary)
        {
            var select = $"SELECT * FROM {table} WHERE Uid=@uid";
            var reader = ExecuteReader(select, new Parameter[]{ new Parameter("@uid", SqliteType.Text, uid)});
            return ReadFirstAsJsonObject(keyTableDictionary, reader, null);
        }

        public string ExecuteGet(string table, long id, Dictionary<string, string> keyTableDictionary)
        {
            var select = $"SELECT * FROM {table} WHERE Id={id}";
            var reader = ExecuteReader(select);
            return ReadFirstAsJsonObject(keyTableDictionary, reader, null);
        }

        public string ExecuteGet(string table, long[] ids, Dictionary<string, string> keyTableDictionary)
        {
            if (ids == null)
            {
                return null;
            }

            if (ids.Length == 0)
            {
                return "[]";
            }

            var expressions = new List<string>();
            foreach (var id in ids)
            {
                expressions.Add($"Id = {id}");
            }

            var where = String.Join(" OR ", expressions);

            var select = $"SELECT * FROM {table} WHERE {where}";
            var reader = ExecuteReader(select);
            return ReadAsJsonArray(keyTableDictionary, reader);
        }

        public string GetId(string queryCommand)
        {
            var id = ReadFirstAsJsonObject(new Dictionary<string, string> { { "id", "Id" } }, ExecuteReader(queryCommand), null);
            return Json.GetKvpValue(id, "id", false);
        }

        public string GetId(string queryCommand, Parameter[] parameters)
        {
            var id = ReadFirstAsJsonObject(new Dictionary<string, string> { { "id", "Id" } }, ExecuteReader(queryCommand, parameters), null);
            return Json.GetKvpValue(id, "id", false);
        }

        public bool ExecuteDelete(string table, long id)
        {
            var select = $"SELECT * FROM {table} WHERE Id={id}";
            if (CountResults(select) > 0)
            {
                var delete = $"DELETE FROM {table} WHERE Id={id}";
                ExecuteNonQuery(delete);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExecuteDelete(string table, long[] ids)
        {
            if (table == null || ids == null)
            {
                return false;
            }

            if (ids.Length == 0)
            {
                return true;
            }

            var expressions = new List<string>();
            foreach(var id in ids)
            {
                expressions.Add($"Id = {id}");
            }

            var where = String.Join(" OR ", expressions);

            var select = $"SELECT * FROM {table} WHERE {where}";
            if (CountResults(select) > 0)
            {
                var delete = $"DELETE FROM {table} WHERE {where}";
                ExecuteNonQuery(delete);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExecuteDelete(string table, string column, string[] values)
        {
            if (table == null || column == null || values == null)
            {
                return false;
            }

            if (values.Length == 0)
            {
                return true;
            }

            //TODO Add Parameters, nicht notwendig momentan, da keine Nutzereingaben hier verwendet werden.

            var expressions = new List<string>();
            foreach (var v in values)
            {
                expressions.Add($"{column} = {v}");
            }

            var where = String.Join(" OR ", expressions);

            var select = $"SELECT * FROM {table} WHERE {where}";
            if (CountResults(select) > 0)
            {
                var delete = $"DELETE FROM {table} WHERE {where}";
                ExecuteNonQuery(delete);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Inserts a new entry into a table.
        /// This method is only compatable with tables that use SqliteTypes Integer and Text.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="keyValuePairs">A dictionary with the column as key and its value.</param>
        /// <returns></returns>
        public long ExecutePost(string table, Dictionary<string, string> keyValuePairs)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            var columns = String.Join(", ", keyValuePairs.Keys);
            var v = new List<string>();
            foreach(var kvp in keyValuePairs)
            {
                if (kvp.Value != null)
                {
                    v.Add($"@{kvp.Key}");
                }
                else
                {
                    v.Add("NULL");
                }
            }

            var values = String.Join(", ", v);
            

            var command = $"INSERT INTO {table} ({columns}) VALUES ({values})";

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = command;

            foreach(var kvp in keyValuePairs)
            {
                if (kvp.Value == null)
                {
                    c.Parameters.Add(new SqliteParameter($"@{kvp.Key}", DBNull.Value));
                }
                else
                {
                    var p = new SqliteParameter($"@{kvp.Key}", $"{kvp.Value}");
                    switch (Json.CheckValueType(kvp.Value))
                    {
                        case Json.ValueType.Number: p.SqliteType = SqliteType.Integer; break;
                        case Json.ValueType.String: p.SqliteType = SqliteType.Text; p.Value = Json.DeserializeString(kvp.Value); break;
                        case Json.ValueType.Invalid: p.SqliteType = SqliteType.Text; break;
                    }

                    if (Json.DeserializeString(Json.DeserializeString(p.Value.ToString().ToLower())) == null)
                    {
                        return -1;
                    }

                    c.Parameters.Add(p);
                }
                
            }

            var success = c.ExecuteNonQuery();

            if (success == 1)
            {
                c.CommandText = @"SELECT last_insert_rowid()";
                var result = (long)c.ExecuteScalar();
                CloseConnection();
                return result;
            }

            return -1;
        }

        public void AddParameters(SqliteCommand command, Parameter[] parameters)
        {
            foreach (var p in parameters)
            {
                command.Parameters.Add(p.parameterName, p.type).Value = p.value;
            }
        }

        public bool ExecutePut(string table, long id, Dictionary<string, string> keyValuePairs)
        {
            if (!File.Exists(Path))
            {
                CreateNewDatabase(false);
            }

            var v = new List<string>();

            var parameters = new List<Parameter>();

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value != null)
                {
                    v.Add($"{kvp.Key} = @{kvp.Key}");
                    var parameterName = $"@{kvp.Key}";
                    var type = SqliteType.Text;
                    var value = kvp.Value;
                    switch (Json.CheckValueType(kvp.Value))
                    {
                        case Json.ValueType.Number: type = SqliteType.Integer; break;
                        case Json.ValueType.String: type = SqliteType.Text; value = Json.DeserializeString(kvp.Value); break;
                        case Json.ValueType.Invalid: type = SqliteType.Text; break;
                    }

                    parameters.Add(new Parameter(parameterName, type, value));
                }
            }

            var values = String.Join(", ", v);


            var command = $"UPDATE {table} SET {values} WHERE Id = {id}";

            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = command;
            AddParameters(c, parameters.ToArray());

            var success = c.ExecuteNonQuery();
            CloseConnection();

            if (success == 1)
            {
                return true;
            }

            return false;
        }


        public long GetMax(string table, string column)
        {
            OpenConnection();
            var c = connection.CreateCommand();
            c.CommandText = $"SELECT MAX({column}) FROM {table}";
            var r = c.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0))
                {
                    var l = r.GetInt64(0);
                    CloseConnection();
                    return l;
                }
                else
                {
                    CloseConnection();
                    return 0;
                }
            }
            else
            {
                CloseConnection();
                return 0;
            }
        }

        public string ReadAsJsonArray(Dictionary<string, string> keyTableDictionary, SqliteDataReader reader, string arrayKey)
        {
            var result = ReadAsJsonArray(keyTableDictionary, reader);
            CloseConnection();
            return Json.SerializeObject(new Dictionary<string, string> { { arrayKey, result } });
        }

        public string ReadAsJsonArray(Dictionary<string, string> keyTableDictionary, SqliteDataReader reader)
        {
            var result = new List<string>();
            using (reader)
                while (reader.Read())
                {
                    var d = new Dictionary<string, string>();
                    foreach(var kvp in keyTableDictionary)
                    {
                        var v = reader[kvp.Value];
                        d[kvp.Key] = Convert.ToString(v);
                    }
                    result.Add(Json.SerializeObject(d));
                }

            CloseConnection();
            return Json.SerializeArray(result.ToArray());
        }

        public string ReadFirstAsJsonObject(Dictionary<string, string> keyTableDictionary, SqliteDataReader reader, string objectKey)
        {
            using (reader)
                if (reader.Read())
                {
                    var d = new Dictionary<string, string>();
                    foreach (var kvp in keyTableDictionary)
                    {
                        var v = reader[kvp.Value];
                        d[kvp.Key] = Convert.ToString(v);
                    }

                    var result = Json.SerializeObject(d);
                    if (objectKey != null)
                    {
                        result = Json.SerializeObject(new Dictionary<string, string> { { objectKey, result } });
                    }

                    CloseConnection();
                    return result;
                }

            CloseConnection();
            return null;
        }

        public const string Path = "aisBuchungen.db";

        public struct Table
        {
            public const string Buchungen = "Buchungen";
            public const string Nutzerdaten = "Nutzerdaten";
            public const string Teilnehmer = "Teilnehmer";
            public const string Veranstalter = "Veranstalter";
            public const string Veranstaltungen = "Veranstaltungen";
        }

        public class Parameter
        {
            public string parameterName;
            public SqliteType type;
            public string value;

            public Parameter(string parameterName, SqliteType type, string value)
            {
                if (parameterName[0] == '@')
                {
                    this.parameterName = parameterName;
                }
                else
                {
                    this.parameterName = $"@{parameterName}";
                }
                
                this.type = type;
                this.value = value;
            }
        }
    }
}
