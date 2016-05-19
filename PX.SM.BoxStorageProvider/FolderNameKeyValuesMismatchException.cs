using System;

namespace PX.SM.BoxStorageProvider
{
    public class FolderNameKeyValuesMismatchException : Exception
    {
        public FolderNameKeyValuesMismatchException()
        {
        }

        public FolderNameKeyValuesMismatchException(string message) : base(message)
        {
        }


        public FolderNameKeyValuesMismatchException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}