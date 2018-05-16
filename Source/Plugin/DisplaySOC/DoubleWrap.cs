namespace DisplaySOC
{
    public class DoubleWrap
    {
        private double _value;
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public DoubleWrap(double value)
        {
            _value = value;
        }

        public DoubleWrap()
        {
            _value = 0;
        }

    }
}
