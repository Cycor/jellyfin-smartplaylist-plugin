using System;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    public class Expression
    {
        public string MemberName { get; set; }
        public string Operator { get; set; }
        public string TargetValue { get; set; }
        public bool Invert { get; set; }
        public bool IgnoreCase { get; set; }

        private Func<Operand, bool> _execute;


        public Expression(string MemberName, string Operator, string TargetValue, bool invert = false, bool ignoreCase = false)
        {
            this.MemberName = MemberName;
            this.Operator = Operator;
            this.TargetValue = TargetValue;
            this.IgnoreCase = ignoreCase;
        }

        public bool Execute(Operand o)
        {
            if(_execute == null)
                _execute = Engine.CompileRule<Operand>(this);

            if(_execute != null)
                return _execute(o);
            return false;
        }
    }
}
