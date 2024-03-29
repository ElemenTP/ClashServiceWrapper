﻿//Code from WinSW: https://github.com/winsw/WinSW
namespace ClashServiceWrapper
{
    internal sealed class CommandException : Exception
    {
        internal CommandException(Exception inner)
            : base(inner.Message, inner)
        {
        }

        internal CommandException(string message)
            : base(message)
        {
        }

        internal CommandException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
