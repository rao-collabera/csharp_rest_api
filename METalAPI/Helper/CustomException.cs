using System;

namespace METalAPI
{
    /// <summary>
    /// Custom Exception Class.
    /// </summary>
    [Serializable]
    public class CustomException : Exception
    {
        /// <summary>
        /// StatusCode
        /// </summary>
        public int StatusCode = 0;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CustomException() : base() { }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="Code">The code.</param>
        /// <param name="message">The message.</param>
        public CustomException(int Code, string message) : base(message) { StatusCode = Code; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="Code">The code.</param>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public CustomException(int Code, string message, Exception inner) : base(message, inner) { StatusCode = Code; }
    }
}