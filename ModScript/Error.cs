
namespace ModScript
{
    class Error
    {
        public string type, message;
        public TextPosition position;
        public Context context;
        public Error() { }
        public Error(TextPosition p) { position = p; }
        public Error(TextPosition p, string msg) { position = p; message = msg; }
        public override string ToString()
        {
            string res = "";
            if (context != null)
            {
                TextPosition pos = position;
                Context ctx = context;
                while (ctx != null)
                {
                    res = $"\tFile {pos.fName}, line {pos.line + 1}, in {ctx.name}\n{res}";
                    pos = ctx.parentEntry;
                    ctx = ctx.parent;
                }
                return $"Traceback:\n{res}{type} at line {position.line + 1} char {position.character + 1} in {position.fName}:\n{message}";
            }
            if (position == null)
                return type;
            if (message == null)
                return $"{res}{type} at line {position.line + 1} char {position.character + 1} in {position.fName}";
            return $"{res}{type} at line {position.line + 1} char {position.character + 1} in {position.fName}:\n{message}";
       }
    }

    class CharacterNotRecognizedError : Error
    {
        public CharacterNotRecognizedError() 
        { type = "CharacterNotRecognized"; }
        public CharacterNotRecognizedError(TextPosition p) : base(p)
        { type = "CharacterNotRecognized"; }
        public CharacterNotRecognizedError(TextPosition p, string msg) : base(p, msg) 
        { type = "CharacterNotRecognized"; }
    }
    class BadFormatingError : Error
    {
        public BadFormatingError() { type = "BadFormating"; }
        public BadFormatingError(TextPosition p) : base(p)
        { type = "BadFormating"; }
        public BadFormatingError(TextPosition p, string msg) : base(p, msg)
        { type = "BadFormating"; }
    }

    class InvalidSyntaxError : Error
    {
        public InvalidSyntaxError() { type = "InvalidSyntax"; }
        public InvalidSyntaxError(TextPosition p) : base(p)
        { type = "InvalidSyntax"; }
        public InvalidSyntaxError(TextPosition p, string msg) : base(p, msg)
        { type = "InvalidSyntax"; }
    }

    class RuntimeError : Error
    {
        public RuntimeError() { type = "Runtime"; }
        public RuntimeError(TextPosition p) : base(p)
        { type = "Runtime"; }
        public RuntimeError(TextPosition p, string msg) : base(p, msg)
        { type = "Runtime"; }
        public RuntimeError(TextPosition p, string msg, Context _context) : base(p, msg)
        { type = "Runtime"; context = _context; }
    }
}
