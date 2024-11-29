namespace Sentinel.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IsFixedLengthAttribute : Attribute
    {
        private bool _isFixedLength;
        public IsFixedLengthAttribute()
        {
            _isFixedLength = true;
        }
        public virtual bool IsFixedLength
        {
            get { return _isFixedLength; }
            set { _isFixedLength = value; }
        }
    }
}
