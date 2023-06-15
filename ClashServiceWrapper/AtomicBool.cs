namespace ClashServiceWrapper
{
    internal class AtomicBool
    {
        private long _value;

        public AtomicBool(bool value = false)
        {
            _value = Convert.ToInt64(value);
        }

        public bool Get()
        {
            return Interlocked.Read(ref _value) == 1;
        }

        public void Set(bool value)
        {
            Interlocked.Exchange(ref _value, Convert.ToInt64(value));
        }
    }
}
