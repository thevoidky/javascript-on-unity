using System.Text.RegularExpressions;
using Jint;

namespace Modules.Runtime
{
    public abstract class JavascriptEngine
    {
        private readonly Engine _engine;
        private readonly Regex _comment;

        protected JavascriptEngine()
        {
            _engine = new Engine();
            _engine.SetValue("window", this);
            _comment = new Regex(@"(^import .*['""`].+[/\\]([.][^./\\]+?)['""`])", RegexOptions.Compiled);
        }

        public void Run(string source)
        {
            source = _comment.Replace(source, "// $1");

            _engine.Execute(source);
        }
    }
}