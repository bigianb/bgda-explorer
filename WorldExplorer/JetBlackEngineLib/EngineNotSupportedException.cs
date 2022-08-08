namespace JetBlackEngineLib
{
    public class EngineNotSupportedException : Exception
    {
        public EngineNotSupportedException(string message) : base(message)
        {
        }

        public EngineNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public EngineNotSupportedException(EngineVersion version) : base($"Unsupported engine version: {version}")
        {
        }
    }
}