using Mono.Data.Sqlite;
using System.Data;
using UnityEngine;
using UnityEngine.UI;

public class DatabaseManager : MonoBehaviour
{
    string conn, sqlQuery;
    IDbConnection dbconn;
    IDbCommand dbcmd;
    [SerializeField] InputField themeInput, questionInput; // Create Question
    [SerializeField] InputField amountInput, fromThemeInput, toThemeInput; // Create Quiz
    [SerializeField] Text totalSavedQuestions;
    [SerializeField] GameObject quizPanel; // Show Quiz
    [SerializeField] InputField quizQuestionIndex, quizThemeInput, quizQuestionInput; // Show Quiz
    [SerializeField] Text quizQuestionAnsweredAttempts, functionResultText; // Show Quiz

    string DATABASE_NAME = "QuizManager.s3db";
    string TABLE_QUESTIONS = "questions";
    string COLUMN_THEME = "theme";
    string COLUMN_QUESTION = "question";
    string COLUMN_ATTEMPTS = "attempts";
    string COLUMN_ANSWERED = "answered";

    int[] quizId;
    float[] quizTheme;
    string[] quizQuestion;
    int[] quizAttempts;
    int[] quizAnswered;
    int currentQuestion;

    void Start()
    {
        string filepath = Application.dataPath + "/Plugins/" + DATABASE_NAME;
        conn = "URI=file:" + filepath;
        dbconn = new SqliteConnection(conn);
        CreateTable();
        CountSavedQuestions();
    }

    private void CreateTable()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            sqlQuery = "CREATE TABLE IF NOT EXISTS [questions] (" +
                "[id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT," +
                "[theme] FLOAT  NOT NULL," +
                "[question] VARCHAR(255)  NOT NULL," +
                "[attempts] INTEGER DEFAULT '0' NOT NULL," +
                "[answered] INTEGER DEFAULT '0' NOT NULL)";
            dbcmd.CommandText = sqlQuery;
            dbcmd.ExecuteScalar();
            dbconn.Close();
        }
    }

    private void CountSavedQuestions()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            IDbCommand dbcmd = dbconn.CreateCommand();
            string sqlQuery = "SELECT COUNT(" + COLUMN_QUESTION + ") FROM " + TABLE_QUESTIONS;
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();

            reader.Read();
            totalSavedQuestions.text = "Total preguntas guardadas: " + reader.GetInt32(0);

            dbconn.Close();
        }
    }

    public void InsertNewQuestion()
    {
        themeInput.text = themeInput.text.Replace(',', '.');
        questionInput.text = questionInput.text.Replace('"', '\"');

        string theme = themeInput.text;
        string question = questionInput.text;

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            sqlQuery = string.Format("INSERT INTO " + TABLE_QUESTIONS + " (" + COLUMN_THEME + ", " + COLUMN_QUESTION + ", " + COLUMN_ATTEMPTS + ", " + COLUMN_ANSWERED + ") VALUES (\"{0}\", \"{1}\", 0, 0)", theme, question);
            dbcmd.CommandText = sqlQuery;
            int rowsAffected = dbcmd.ExecuteNonQuery();

            dbconn.Close();

            if (rowsAffected == 1)
            {
                functionResultText.text = "Se ha guardado una nueva pregunta";
                functionResultText.color = Color.green;
                functionResultText.gameObject.SetActive(true);
            }
            else
            {
                functionResultText.text = "No se pudo guardar la nueva pregunta";
                functionResultText.color = Color.red;
                functionResultText.gameObject.SetActive(true);
            }
        }

        CountSavedQuestions();
    }

    public void SelectQuestionsByFilters()
    {
        fromThemeInput.text = fromThemeInput.text.Replace(',', '.');
        toThemeInput.text = toThemeInput.text.Replace(',', '.');

        int amount = int.Parse(amountInput.text != "" ? amountInput.text : "10");
        float fromTheme = float.Parse(fromThemeInput.text != "" ? fromThemeInput.text : "0.0");
        float toTheme = float.Parse(toThemeInput.text != "" ? toThemeInput.text : "0.0");

        string whereFromTheme = fromTheme != 0.0f ? (COLUMN_THEME + " >= " + fromTheme) : "";
        string whereToTheme = toTheme != 0.0f ? (COLUMN_THEME + " <= " + toTheme) : "";
        string whereConditions = "";

        if (whereFromTheme.Length > 0 || whereToTheme.Length > 0)
        {
            whereConditions = " WHERE ";

            if (whereFromTheme.Length > 0)
            {
                whereConditions += whereFromTheme;

                if (whereToTheme.Length > 0)
                {
                    whereConditions += " AND " + whereToTheme;
                }
            }
            else
            {
                whereConditions += whereToTheme;
            }
        }

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            IDbCommand dbcmd = dbconn.CreateCommand();
            string sqlQueryCount = "SELECT * FROM " + TABLE_QUESTIONS + whereConditions + " LIMIT " + amount;
            dbcmd.CommandText = sqlQueryCount;
            IDataReader readerCount = dbcmd.ExecuteReader();

            int numRows = 0;
            while (readerCount.Read())
            {
                numRows++;
            }
            readerCount.Close();

            sqlQuery = "SELECT * FROM " + TABLE_QUESTIONS + whereConditions + " ORDER BY RANDOM() LIMIT " + amount;
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();

            quizId = new int[numRows];
            quizTheme = new float[numRows];
            quizQuestion = new string[numRows];
            quizAttempts = new int[numRows];
            quizAnswered = new int[numRows];

            int index = 0;

            while (reader.Read())
            {
                quizId[index] = reader.GetInt32(0);
                quizTheme[index] = reader.GetFloat(1);
                quizQuestion[index] = reader.GetString(2);
                quizAttempts[index] = reader.GetInt32(3);
                quizAnswered[index] = reader.GetInt32(4);

                index++;
            }

            dbconn.Close();

            currentQuestion = -1;
            ShowQuiz();
        }
    }

    private void ShowQuiz()
    {
        currentQuestion++;

        if (currentQuestion < quizId.Length)
        {
            quizPanel.SetActive(true);

            quizQuestionIndex.GetComponentInChildren<Text>().text = "Pregunta " + (currentQuestion + 1) + "/" + quizId.Length;
            quizThemeInput.text = "Tema: " + quizTheme[currentQuestion];
            quizQuestionInput.text = quizQuestion[currentQuestion];
            quizQuestionAnsweredAttempts.text = "Intentos/Respondidos: " + quizAnswered[currentQuestion] + "/" + quizAttempts[currentQuestion];
        }
        else
        {
            quizPanel.SetActive(false);
            functionResultText.text = "Cuestionario finalizado";
            functionResultText.color = Color.white;
            functionResultText.gameObject.SetActive(true);
        }
    }

    public void QuestionAnswered(bool wasAnswered)
    {
        string setAnswered = "";

        if (wasAnswered)
        {
            setAnswered = COLUMN_ANSWERED + " = @answered, ";
        }

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            sqlQuery = string.Format("UPDATE " + TABLE_QUESTIONS + " SET " + setAnswered + COLUMN_ATTEMPTS + " = @attempts WHERE id = " + quizId[currentQuestion]);
            
            if (wasAnswered)
            {
                SqliteParameter answered = new SqliteParameter("@answered", quizAnswered[currentQuestion] + 1);
                dbcmd.Parameters.Add(answered);
            }

            SqliteParameter attempts = new SqliteParameter("@attempts", quizAttempts[currentQuestion] + 1);
            dbcmd.Parameters.Add(attempts);

            dbcmd.CommandText = sqlQuery;
            int rowsAffected = dbcmd.ExecuteNonQuery();

            dbconn.Close();

            if (rowsAffected == 1)
            {

            }
        }

        ShowQuiz();
    }

    public void UpdateQuestion()
    {
        if (quizThemeInput.text.Contains("Tema"))
        {
            quizThemeInput.text = quizThemeInput.text.Split(':')[1];
        }

        quizThemeInput.text = quizThemeInput.text.Replace(',', '.');

        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            dbcmd = dbconn.CreateCommand();
            sqlQuery = string.Format("UPDATE " + TABLE_QUESTIONS + " SET " + COLUMN_THEME + " = @theme, " + COLUMN_QUESTION + " = @question WHERE id = " + quizId[currentQuestion]);

            SqliteParameter theme = new SqliteParameter("@theme", quizThemeInput.text);
            SqliteParameter question = new SqliteParameter("@question", quizQuestionInput.text);

            dbcmd.Parameters.Add(theme);
            dbcmd.Parameters.Add(question);

            dbcmd.CommandText = sqlQuery;
            int rowsAffected = dbcmd.ExecuteNonQuery();

            dbconn.Close();

            if (rowsAffected == 1)
            {
                
            }
        }
    }

    public void DeleteQuestion()
    {
        using (dbconn = new SqliteConnection(conn))
        {
            dbconn.Open();
            IDbCommand dbcmd = dbconn.CreateCommand();
            string sqlQuery = "DELETE FROM " + TABLE_QUESTIONS + " WHERE id = " + quizId[currentQuestion];
            dbcmd.CommandText = sqlQuery;
            int rowsAffected = dbcmd.ExecuteNonQuery();

            dbconn.Close();

            if (rowsAffected == 1)
            {
                functionResultText.text = "Se ha eliminado la pregunta";
                functionResultText.color = Color.green;
                functionResultText.gameObject.SetActive(true);
            }
        }

        CountSavedQuestions();
        ShowQuiz();
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
