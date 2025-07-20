namespace Utils {
    public class ConsoleOutput {
        public static bool Verbose = false;
        public static bool ShowWarnings = true;
        
        private static readonly List<string> PrintQueue = [];
        
        public int Id { get; }
        
        public int Indent { get; set; }
        
        public ConsoleOutput() {
            Id = PrintQueue.Count;
            Indent = 0;
            PrintQueue.Add("");
        }
        
        public ConsoleOutput(int indent)
            : this() {
            Indent = indent;
        }
        
        public void Write(string text) {
            PrintQueue[Id] += new string(' ', Indent) + text;
        }
        
        public void WriteLine(string text = "") {
            Write(text + "\n");
        }
        
        public void WriteIfVerbose(string text) {
            if (Verbose) {
                PrintQueue[Id] += new string(' ', Indent) + text;
            }
        }
        
        public void WriteLineIfVerbose(string text = "") {
            WriteIfVerbose(text + "\n");
        }
        
        public void Flush() {
            Console.WriteLine(PrintQueue[Id]);
            PrintQueue[Id] = "";
        }
        
        public static void Clear() {
            PrintQueue.Clear();
        }
    }
}
