class Logger
{
    private readonly string logFilePath;

    public Logger(string filePath)
    {
        logFilePath = filePath;

        // Create the file if it doesn't exist
        if (!File.Exists(logFilePath))
        {
            using (File.Create(logFilePath)) { }
        }
    }

    public void Log(string message)
    {
        string logEntry = $"{message}";
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }
}


