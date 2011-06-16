using System;

namespace NetShit
{
    [Serializable]
    public class _ErrorException : Exception
    {
        public string ErrorMessage
        {
            get
            {
                return base.Message.ToString();
            }
        }

        public _ErrorException(string errorMessage)
            : base(errorMessage) { }

        public _ErrorException(string errorMessage, Exception innerEx)
            : base(errorMessage, innerEx) { }
    }

    public class DummyException : _ErrorException
    {
        public DummyException(string errorMessage)
            : base(errorMessage) { }

        public DummyException(string errorMessage, Exception innerEx)
            : base(errorMessage, innerEx) { }
    }

    public class HeyDummyInitializeYouVariablesBeforeYouTryAndUseThemDUH : _ErrorException
    {
        public HeyDummyInitializeYouVariablesBeforeYouTryAndUseThemDUH(string errorMessage)
            : base(errorMessage) { }

        public HeyDummyInitializeYouVariablesBeforeYouTryAndUseThemDUH(string errorMessage, Exception innerEx)
            : base(errorMessage, innerEx) { }
    }
}