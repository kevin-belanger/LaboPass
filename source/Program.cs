namespace LaboPass;

static class Program
{
    private const string SingleInstanceMutexName = @"Global\LaboPass-0B1C5DC1-1A3F-40EE-9A77-8B8A91E5E9D8";

    [STAThread]
    static void Main()
    {
        using Mutex singleInstanceMutex = new(true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "LaboPass est déjà ouvert.",
                "LaboPass",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}
