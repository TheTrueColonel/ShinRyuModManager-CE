namespace Utils {
    public class ConsoleOutput {
        public static bool Verbose = false;
        public static bool ShowWarnings = true;
        
        private static readonly List<string> PrintQueue = [];

        private readonly int _id;
        private readonly int _indent;
        
        public ConsoleOutput() {
            _id = PrintQueue.Count;
            _indent = 0;
            PrintQueue.Add("");
        }
        
        public ConsoleOutput(int indent) : this() {
            _indent = indent;
        }
        
        public void Write(string text) {
            PrintQueue[_id] += new string(' ', _indent) + text;
        }
        
        public void WriteLine(string text = "") {
            Write(text + "\n");
        }
        
        public void WriteIfVerbose(string text) {
            if (Verbose) {
                PrintQueue[_id] += new string(' ', _indent) + text;
            }
        }
        
        public void WriteLineIfVerbose(string text = "") {
            WriteIfVerbose(text + "\n");
        }
        
        public void Flush() {
            Console.WriteLine(PrintQueue[_id]);
            PrintQueue[_id] = "";
        }
        
        public static void Clear() {
            PrintQueue.Clear();
        }
    }
}
