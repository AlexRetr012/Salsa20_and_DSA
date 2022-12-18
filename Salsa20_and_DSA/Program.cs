namespace Salsa20_and_DSA
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
    public static class CallBackMy
    {
        public delegate void callbackEvent(string p,string q,string g,string text,
            string hashtext,string openkey,string r,string s,string h);
        public static callbackEvent callbackEventHandler;
    }
}