namespace Translumo.MVVM.Models
{
    public class LocalServerURL
    {
        public string Value { get; set; }

        public LocalServerURL(string value)
        {
            Value = value;
        }
        override public string ToString()
        {
            return Value;
        }
    }

    public class LocalServerPayload
    {
        public string Value { get; set; }

        public LocalServerPayload(string value)
        {
            Value = value;
        }
        override public string ToString()
        {
            return Value;
        }
    }
    
    public class LocalServerResponsePath
    {
        public string Value { get; set; }

        public LocalServerResponsePath(string value)
        {
            Value = value;
        }
        override public string ToString()
        {
            return Value;
        }
    }
}
