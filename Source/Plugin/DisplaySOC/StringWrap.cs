using System;

namespace DisplaySOC
{
    public class StringWrap
    {
        private string _value;
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public StringWrap(string value)
        {
            _value = value;
        }

        public StringWrap()
        {
            _value = String.Empty;
        }

    }
}

