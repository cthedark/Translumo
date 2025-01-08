namespace Translumo.MVVM.Models
{
    public class LocalDBDir
    {
        public string Value { get; set;}

        public LocalDBDir(string value) {
            Value = value;
        }
        override public string ToString() {
            return Value;
        }
    }
}
